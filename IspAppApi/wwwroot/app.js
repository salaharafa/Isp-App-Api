const TOKEN_STORAGE_KEY = "jwt_token";

/**
 * دالة تعقيم النصوص لتجنب ثغرات XSS عند عرض بيانات الخادم
 */
function escapeHtml(str) {
    if (str == null) return "";
    return String(str)
        .replace(/&/g, "&amp;")
        .replace(/</g, "&lt;")
        .replace(/>/g, "&gt;")
        .replace(/"/g, "&quot;")
        .replace(/'/g, "&#039;");
}

/**
 * دالة تنسيق التاريخ والتعامل مع التواريخ الفارغة (null)
 */
function formatDate(dateValue) {
    if (!dateValue) return '<span class="text-muted">—</span>';
    try {
        const d = new Date(dateValue);
        if (isNaN(d.getTime())) return escapeHtml(String(dateValue));
        return d.toISOString().split("T")[0];
    } catch {
        return escapeHtml(String(dateValue));
    }
}

/**
 * دالة استخراج وتنسيق حالة الاشتراك الفعلية من الـ DTO
 */
function formatSubscriptionStatus(status) {
    if (status === 0 || status === "Active" || status === "active") {
        return { text: "نشط (Active)", class: "badge-status-active" };
    }
    if (status === 1 || status === "Suspended" || status === "suspended") {
        return { text: "معلق (Suspended)", class: "badge-status-suspended" };
    }
    if (status === 2 || status === "Cancelled" || status === "cancelled") {
        return { text: "ملغي (Cancelled)", class: "badge-status-cancelled" };
    }
    return { text: escapeHtml(String(status ?? "غير محدد")), class: "badge-status-inactive" };
}

/**
 * دالة استخراج وتنسيق حالة دفع الفاتورة الفعلية كما يرجعها الـ DTO
 */
function formatPaymentStatus(status) {
    const s = String(status || "").trim();
    if (s.toLowerCase() === "paid" || s === "مدفوعة") {
        return { text: "مدفوعة (Paid)", class: "badge-status-active" };
    }
    if (s.toLowerCase() === "unpaid" || s === "غير مدفوعة") {
        return { text: "غير مدفوعة (Unpaid)", class: "badge-status-cancelled" };
    }
    return { text: escapeHtml(s || "غير محدد"), class: "badge-status-inactive" };
}

/**
 * دالة استخراج وتنسيق حالة سجل الدفع الفعلية من PaymentReadDTO
 */
function formatPaymentRecordStatus(isCancelled, cancellationReason, cancelledAt) {
    if (isCancelled) {
        let details = "";
        if (cancellationReason) {
            details += `سبب الإلغاء: ${escapeHtml(cancellationReason)}`;
        }
        if (cancelledAt) {
            const formattedCancelledDate = formatDate(cancelledAt);
            details += (details ? " - " : "") + `بتاريخ: ${formattedCancelledDate}`;
        }
        return {
            badgeClass: "badge-status-cancelled",
            badgeText: "ملغاة (Cancelled)",
            details: details
        };
    }
    return {
        badgeClass: "badge-status-active",
        badgeText: "مكتملة (Completed)",
        details: ""
    };
}

/**
 * فك ترميز بيانات حمولة التوكن (JWT Payload) لاستخراج المعلومات الحالية
 */
function parseJwt(token) {
    try {
        const parts = token.split(".");
        if (parts.length < 2) return null;
        const base64Url = parts[1];
        const base64 = base64Url.replace(/-/g, "+").replace(/_/g, "/");
        const pad = base64.length % 4;
        const paddedBase64 = pad ? base64 + "=".repeat(4 - pad) : base64;
        const jsonPayload = decodeURIComponent(
            atob(paddedBase64)
                .split("")
                .map((c) => "%" + ("00" + c.charCodeAt(0).toString(16)).slice(-2))
                .join("")
        );
        return JSON.parse(jsonPayload);
    } catch {
        return null;
    }
}

/**
 * التحقق من انتهاء صلاحية التوكن إذا كانت خاصية exp متوفرة
 */
function isTokenExpired(token) {
    const payload = parseJwt(token);
    if (!payload || !payload.exp) return false;
    return Date.now() >= payload.exp * 1000;
}

/**
 * دالة مشتركة لإرسال طلبات الـ API المحمية
 * تضيف تلقائياً ترويسة Authorization: Bearer {token}
 * وتتعامل مع استجابة 401 Unauthorized بإنهاء الجلسة وإعادة المستخدم لنموذج الدخول
 */
async function authFetch(url, options = {}) {
    const token = sessionStorage.getItem(TOKEN_STORAGE_KEY);
    const headers = new Headers(options.headers || {});

    if (token) {
        headers.set("Authorization", `Bearer ${token}`);
    }

    const response = await fetch(url, {
        ...options,
        headers: headers
    });

    if (response.status === 401) {
        if (typeof window.handleLogout === "function") {
            window.handleLogout("انتهت صلاحية الجلسة، يرجى تسجيل الدخول مجدداً.", "error");
        } else {
            sessionStorage.removeItem(TOKEN_STORAGE_KEY);
            window.location.reload();
        }
    }

    return response;
}

// إتاحة دالة الطلبات المحمية بشكل عام للاستخدام المستقبلي
window.authFetch = authFetch;

document.addEventListener("DOMContentLoaded", () => {
    // حالة بيانات العملاء لتجنب إعادة الطلب غير المبررة
    let customersLoaded = false;
    let isCustomersLoading = false;

    // حالة بيانات باقات الإنترنت
    let plansLoaded = false;
    let isPlansLoading = false;

    // حالة بيانات الاشتراكات
    let subscriptionsLoaded = false;
    let isSubscriptionsLoading = false;

    // حالة بيانات الفواتير
    let billsLoaded = false;
    let isBillsLoading = false;
    let lastLoadedBillCustomerId = null;

    // حالة بيانات المدفوعات
    let paymentsLoaded = false;
    let isPaymentsLoading = false;
    let cachedPayments = [];

    // عناصر واجهة تسجيل الدخول
    const loginView = document.getElementById("loginView");
    const loginForm = document.getElementById("loginForm");
    const usernameInput = document.getElementById("username");
    const passwordInput = document.getElementById("password");
    const submitBtn = document.getElementById("submitBtn");
    const btnText = submitBtn.querySelector(".btn-text");
    const spinner = submitBtn.querySelector(".spinner");
    const alertBox = document.getElementById("alertBox");

    // عناصر لوحة التحكم
    const dashboardView = document.getElementById("dashboardView");
    const sidebar = document.getElementById("sidebar");
    const sidebarToggle = document.getElementById("sidebarToggle");
    const topbarTitle = document.getElementById("topbarTitle");
    const sessionUser = document.getElementById("sessionUser");
    const logoutBtn = document.getElementById("logoutBtn");
    const navItems = document.querySelectorAll(".nav-item[data-section]");
    const sections = document.querySelectorAll(".dashboard-section");

    // عناصر قسم العملاء
    const refreshCustomersBtn = document.getElementById("refreshCustomersBtn");
    const retryCustomersBtn = document.getElementById("retryCustomersBtn");
    const customersLoading = document.getElementById("customersLoading");
    const customersError = document.getElementById("customersError");
    const customersErrorMessage = document.getElementById("customersErrorMessage");
    const customersEmpty = document.getElementById("customersEmpty");
    const customersTableWrapper = document.getElementById("customersTableWrapper");
    const customersTableBody = document.getElementById("customersTableBody");

    // عناصر قسم باقات الإنترنت
    const refreshPlansBtn = document.getElementById("refreshPlansBtn");
    const retryPlansBtn = document.getElementById("retryPlansBtn");
    const plansLoading = document.getElementById("plansLoading");
    const plansError = document.getElementById("plansError");
    const plansErrorMessage = document.getElementById("plansErrorMessage");
    const plansEmpty = document.getElementById("plansEmpty");
    const plansTableWrapper = document.getElementById("plansTableWrapper");
    const plansTableBody = document.getElementById("plansTableBody");

    // عناصر قسم الاشتراكات
    const refreshSubscriptionsBtn = document.getElementById("refreshSubscriptionsBtn");
    const retrySubscriptionsBtn = document.getElementById("retrySubscriptionsBtn");
    const subscriptionsLoading = document.getElementById("subscriptionsLoading");
    const subscriptionsError = document.getElementById("subscriptionsError");
    const subscriptionsErrorMessage = document.getElementById("subscriptionsErrorMessage");
    const subscriptionsEmpty = document.getElementById("subscriptionsEmpty");
    const subscriptionsTableWrapper = document.getElementById("subscriptionsTableWrapper");
    const subscriptionsTableBody = document.getElementById("subscriptionsTableBody");

    // عناصر قسم الفواتير
    const billCustomerIdInput = document.getElementById("billCustomerIdInput");
    const refreshBillsBtn = document.getElementById("refreshBillsBtn");
    const retryBillsBtn = document.getElementById("retryBillsBtn");
    const billsLoading = document.getElementById("billsLoading");
    const billsError = document.getElementById("billsError");
    const billsErrorMessage = document.getElementById("billsErrorMessage");
    const billsEmpty = document.getElementById("billsEmpty");
    const billsTableWrapper = document.getElementById("billsTableWrapper");
    const billsTableBody = document.getElementById("billsTableBody");

    // عناصر قسم المدفوعات
    const paymentBillFilterInput = document.getElementById("paymentBillFilterInput");
    const refreshPaymentsBtn = document.getElementById("refreshPaymentsBtn");
    const retryPaymentsBtn = document.getElementById("retryPaymentsBtn");
    const paymentsLoading = document.getElementById("paymentsLoading");
    const paymentsError = document.getElementById("paymentsError");
    const paymentsErrorMessage = document.getElementById("paymentsErrorMessage");
    const paymentsEmpty = document.getElementById("paymentsEmpty");
    const paymentsEmptyMessage = document.getElementById("paymentsEmptyMessage");
    const paymentsTableWrapper = document.getElementById("paymentsTableWrapper");
    const paymentsTableBody = document.getElementById("paymentsTableBody");

    function showAlert(message, type = "error") {
        alertBox.textContent = message;
        alertBox.className = `alert-box alert-${type}`;
        alertBox.hidden = false;
    }

    function hideAlert() {
        alertBox.hidden = true;
        alertBox.textContent = "";
        alertBox.className = "alert-box";
    }

    function setLoading(isLoading) {
        submitBtn.disabled = isLoading;
        if (isLoading) {
            btnText.textContent = "جاري التحقق...";
            spinner.hidden = false;
        } else {
            btnText.textContent = "تسجيل الدخول";
            spinner.hidden = true;
        }
    }

    function showLoginView() {
        loginView.hidden = false;
        dashboardView.hidden = true;
    }

    function showDashboardView(token) {
        loginView.hidden = true;
        dashboardView.hidden = false;

        const payload = parseJwt(token);
        const username = payload?.Username || payload?.username || payload?.sub || "المستخدم";
        sessionUser.textContent = username;

        // تنشيط القسم الافتراضي (الرئيسية)
        switchSection("home");
    }

    /**
     * جلب بيانات العملاء من الـ API الحقيقي وعرضها في الجدول
     * يدعم فحص التخزين المؤقت وحالات التحميل والقائمة الفارغة والخطأ
     */
    async function loadCustomers(force = false) {
        if (isCustomersLoading) return;
        if (customersLoaded && !force) return;

        // تفعيل حالة التحميل وإخفاء باقي الحالات
        customersLoading.hidden = false;
        customersError.hidden = true;
        customersEmpty.hidden = true;
        customersTableWrapper.hidden = true;
        isCustomersLoading = true;

        try {
            const response = await authFetch("/api/Customers");

            if (response.ok) {
                const data = await response.json();
                customersLoaded = true;
                customersLoading.hidden = true;

                if (!Array.isArray(data) || data.length === 0) {
                    customersEmpty.hidden = false;
                } else {
                    customersTableBody.innerHTML = "";
                    data.forEach((customer) => {
                        // استخدام أسماء الخصائص المطابقة لـ CustomerReadDTO: Id, Name, Email
                        const id = customer.id ?? customer.Id ?? "";
                        const name = customer.name ?? customer.Name ?? "";
                        const email = customer.email ?? customer.Email ?? "";

                        const tr = document.createElement("tr");
                        tr.innerHTML = `
                            <td><span class="badge-id">${escapeHtml(String(id))}</span></td>
                            <td class="font-medium text-dark">${escapeHtml(String(name))}</td>
                            <td>${escapeHtml(String(email))}</td>
                        `;
                        customersTableBody.appendChild(tr);
                    });
                    customersTableWrapper.hidden = false;
                }
            } else {
                customersLoading.hidden = true;
                customersError.hidden = false;
                if (customersErrorMessage) {
                    customersErrorMessage.textContent = `حدث خطأ أثناء جلب بيانات العملاء من الخادم (رمز الاستجابة: ${response.status})`;
                }
            }
        } catch (error) {
            customersLoading.hidden = true;
            customersError.hidden = false;
            if (customersErrorMessage) {
                customersErrorMessage.textContent = "تعذر الاتصال بالخادم لجلب بيانات العملاء. يرجى التأكد من تشغيل الخدمة والمحاولة لاحقاً.";
            }
        } finally {
            isCustomersLoading = false;
        }
    }

    /**
     * جلب بيانات باقات الإنترنت من الـ API الحقيقي وعرضها في الجدول
     * يدعم فحص التخزين المؤقت وحالات التحميل والقائمة الفارغة والخطأ
     */
    async function loadServicePlans(force = false) {
        if (isPlansLoading) return;
        if (plansLoaded && !force) return;

        // تفعيل حالة التحميل وإخفاء باقي الحالات
        plansLoading.hidden = false;
        plansError.hidden = true;
        plansEmpty.hidden = true;
        plansTableWrapper.hidden = true;
        isPlansLoading = true;

        try {
            const response = await authFetch("/api/ServicePlans");

            if (response.ok) {
                const data = await response.json();
                plansLoaded = true;
                plansLoading.hidden = true;

                if (!Array.isArray(data) || data.length === 0) {
                    plansEmpty.hidden = false;
                } else {
                    plansTableBody.innerHTML = "";
                    data.forEach((plan) => {
                        // استخدام أسماء الخصائص المطابقة لـ ServicePlanReadDTO: Id, Name, Speed, Price, Status, IsActive
                        const id = plan.id ?? plan.Id ?? "";
                        const name = plan.name ?? plan.Name ?? "";
                        const speed = plan.speed ?? plan.Speed;
                        const price = plan.price ?? plan.Price ?? 0;
                        const status = plan.status ?? plan.Status ?? "";
                        const isActive = Boolean(plan.isActive ?? plan.IsActive);

                        const speedDisplay = (speed !== null && speed !== undefined && speed !== "")
                            ? `${escapeHtml(String(speed))} ميغابت/ث`
                            : "غير محدد";

                        const priceDisplay = typeof price === "number"
                            ? `${price.toFixed(2)}`
                            : escapeHtml(String(price));

                        const tr = document.createElement("tr");
                        tr.innerHTML = `
                            <td><span class="badge-id">${escapeHtml(String(id))}</span></td>
                            <td class="font-medium text-dark">${escapeHtml(String(name))}</td>
                            <td>${speedDisplay}</td>
                            <td>${priceDisplay}</td>
                            <td><span class="badge-status ${isActive ? 'badge-status-active' : 'badge-status-inactive'}">${escapeHtml(String(status))}</span></td>
                            <td><span class="badge-status ${isActive ? 'badge-status-active' : 'badge-status-inactive'}">${isActive ? 'نشطة' : 'غير نشطة'}</span></td>
                        `;
                        plansTableBody.appendChild(tr);
                    });
                    plansTableWrapper.hidden = false;
                }
            } else {
                plansLoading.hidden = true;
                plansError.hidden = false;
                if (plansErrorMessage) {
                    plansErrorMessage.textContent = `حدث خطأ أثناء جلب باقات الإنترنت من الخادم (رمز الاستجابة: ${response.status})`;
                }
            }
        } catch (error) {
            plansLoading.hidden = true;
            plansError.hidden = false;
            if (plansErrorMessage) {
                plansErrorMessage.textContent = "تعذر الاتصال بالخادم لجلب باقات الإنترنت. يرجى التأكد من تشغيل الخدمة والمحاولة لاحقاً.";
            }
        } finally {
            isPlansLoading = false;
        }
    }

    /**
     * جلب بيانات الاشتراكات من الـ API الحقيقي وعرضها في الجدول
     * يدعم فحص التخزين المؤقت وحالات التحميل والقائمة الفارغة والخطأ
     */
    async function loadSubscriptions(force = false) {
        if (isSubscriptionsLoading) return;
        if (subscriptionsLoaded && !force) return;

        // تفعيل حالة التحميل وإخفاء باقي الحالات
        subscriptionsLoading.hidden = false;
        subscriptionsError.hidden = true;
        subscriptionsEmpty.hidden = true;
        subscriptionsTableWrapper.hidden = true;
        isSubscriptionsLoading = true;

        try {
            const response = await authFetch("/api/Subscriptions");

            if (response.ok) {
                const data = await response.json();
                subscriptionsLoaded = true;
                subscriptionsLoading.hidden = true;

                if (!Array.isArray(data) || data.length === 0) {
                    subscriptionsEmpty.hidden = false;
                } else {
                    subscriptionsTableBody.innerHTML = "";
                    data.forEach((sub) => {
                        // استخدام أسماء الخصائص المطابقة لـ SubscriptionReadDTO: Id, Name, StartDate, EndDate, Status, CustomerId, ServicePlanId
                        const id = sub.id ?? sub.Id ?? "";
                        const name = sub.name ?? sub.Name ?? "";
                        const customerId = sub.customerId ?? sub.CustomerId ?? "";
                        const servicePlanId = sub.servicePlanId ?? sub.ServicePlanId ?? "";
                        const startDate = sub.startDate ?? sub.StartDate;
                        const endDate = sub.endDate ?? sub.EndDate;
                        const status = sub.status ?? sub.Status;

                        const statusInfo = formatSubscriptionStatus(status);

                        const tr = document.createElement("tr");
                        tr.innerHTML = `
                            <td><span class="badge-id">${escapeHtml(String(id))}</span></td>
                            <td class="font-medium text-dark">${escapeHtml(String(name))}</td>
                            <td><span class="badge-id">${escapeHtml(String(customerId))}</span></td>
                            <td><span class="badge-id">${escapeHtml(String(servicePlanId))}</span></td>
                            <td>${formatDate(startDate)}</td>
                            <td>${formatDate(endDate)}</td>
                            <td><span class="badge-status ${statusInfo.class}">${statusInfo.text}</span></td>
                        `;
                        subscriptionsTableBody.appendChild(tr);
                    });
                    subscriptionsTableWrapper.hidden = false;
                }
            } else {
                subscriptionsLoading.hidden = true;
                subscriptionsError.hidden = false;
                if (subscriptionsErrorMessage) {
                    subscriptionsErrorMessage.textContent = `حدث خطأ أثناء جلب الاشتراكات من الخادم (رمز الاستجابة: ${response.status})`;
                }
            }
        } catch (error) {
            subscriptionsLoading.hidden = true;
            subscriptionsError.hidden = false;
            if (subscriptionsErrorMessage) {
                subscriptionsErrorMessage.textContent = "تعذر الاتصال بالخادم لجلب الاشتراكات. يرجى التأكد من تشغيل الخدمة والمحاولة لاحقاً.";
            }
        } finally {
            isSubscriptionsLoading = false;
        }
    }

    /**
     * جلب فواتير العميل المحدّد من الـ API الحقيقي وعرضها في الجدول
     * يدعم فحص التخزين المؤقت وحالات التحميل والقائمة الفارغة والخطأ
     */
    async function loadBills(force = false) {
        const customerId = billCustomerIdInput ? billCustomerIdInput.value.trim() : "1";
        if (!customerId) {
            billsLoading.hidden = true;
            billsTableWrapper.hidden = true;
            billsEmpty.hidden = true;
            billsError.hidden = false;
            if (billsErrorMessage) {
                billsErrorMessage.textContent = "يرجى إدخال معرف العميل لعرض فواتيره.";
            }
            return;
        }

        if (isBillsLoading) return;
        if (billsLoaded && !force && lastLoadedBillCustomerId === customerId) return;

        // تفعيل حالة التحميل وإخفاء باقي الحالات
        billsLoading.hidden = false;
        billsError.hidden = true;
        billsEmpty.hidden = true;
        billsTableWrapper.hidden = true;
        isBillsLoading = true;

        try {
            const response = await authFetch(`/api/customers/${customerId}/bills`);

            if (response.ok) {
                const data = await response.json();
                billsLoaded = true;
                lastLoadedBillCustomerId = customerId;
                billsLoading.hidden = true;

                if (!Array.isArray(data) || data.length === 0) {
                    billsEmpty.hidden = false;
                } else {
                    billsTableBody.innerHTML = "";
                    data.forEach((bill) => {
                        // استخدام أسماء الخصائص المطابقة لـ BillReadDTO: Id, TotalAmount, DueDate, PaymentStatus
                        const id = bill.id ?? bill.Id ?? "";
                        const totalAmount = bill.totalAmount ?? bill.TotalAmount ?? 0;
                        const dueDate = bill.dueDate ?? bill.DueDate;
                        const paymentStatus = bill.paymentStatus ?? bill.PaymentStatus ?? "";

                        const statusInfo = formatPaymentStatus(paymentStatus);
                        const amountDisplay = typeof totalAmount === "number"
                            ? `${totalAmount.toFixed(2)} ر.س`
                            : `${escapeHtml(String(totalAmount))} ر.س`;

                        const tr = document.createElement("tr");
                        tr.innerHTML = `
                            <td><span class="badge-id">${escapeHtml(String(id))}</span></td>
                            <td class="font-medium text-dark">${amountDisplay}</td>
                            <td>${formatDate(dueDate)}</td>
                            <td><span class="badge-status ${statusInfo.class}">${statusInfo.text}</span></td>
                        `;
                        billsTableBody.appendChild(tr);
                    });
                    billsTableWrapper.hidden = false;
                }
            } else {
                billsLoading.hidden = true;
                billsError.hidden = false;
                if (billsErrorMessage) {
                    billsErrorMessage.textContent = `حدث خطأ أثناء جلب الفواتير من الخادم (رمز الاستجابة: ${response.status})`;
                }
            }
        } catch (error) {
            billsLoading.hidden = true;
            billsError.hidden = false;
            if (billsErrorMessage) {
                billsErrorMessage.textContent = "تعذر الاتصال بالخادم لجلب الفواتير. يرجى التأكد من تشغيل الخدمة والمحاولة لاحقاً.";
            }
        } finally {
            isBillsLoading = false;
        }
    }

    /**
     * عرض سجلات المدفوعات في الجدول مع تطبيق تصفية رقم الفاتورة إن وجدت
     */
    function renderPaymentsTable() {
        const filterVal = paymentBillFilterInput ? paymentBillFilterInput.value.trim() : "";
        let items = cachedPayments;

        if (filterVal) {
            items = cachedPayments.filter((p) => {
                const billId = String(p.billId ?? p.BillId ?? "");
                return billId === filterVal;
            });
        }

        if (items.length === 0) {
            paymentsTableWrapper.hidden = true;
            if (filterVal) {
                paymentsEmptyMessage.textContent = `لا توجد مدفوعات مسجلة للفاتورة رقم (${escapeHtml(filterVal)}).`;
            } else {
                paymentsEmptyMessage.textContent = "لا توجد سجلات مدفوعات مسجلة حالياً في النظام.";
            }
            paymentsEmpty.hidden = false;
        } else {
            paymentsEmpty.hidden = true;
            paymentsTableBody.innerHTML = "";

            items.forEach((payment) => {
                const id = payment.id ?? payment.Id ?? "";
                const amount = payment.amount ?? payment.Amount ?? 0;
                const paymentDate = payment.paymentDate ?? payment.PaymentDate;
                const billId = payment.billId ?? payment.BillId ?? "";
                const isCancelled = Boolean(payment.isCancelled ?? payment.IsCancelled);
                const cancellationReason = payment.cancellationReason ?? payment.CancellationReason;
                const cancelledAt = payment.cancelledAt ?? payment.CancelledAt;

                const statusInfo = formatPaymentRecordStatus(isCancelled, cancellationReason, cancelledAt);
                const amountDisplay = typeof amount === "number"
                    ? `${amount.toFixed(2)} ر.س`
                    : `${escapeHtml(String(amount))} ر.س`;

                const tr = document.createElement("tr");
                tr.innerHTML = `
                    <td><span class="badge-id">${escapeHtml(String(id))}</span></td>
                    <td class="font-medium text-dark">${amountDisplay}</td>
                    <td>${formatDate(paymentDate)}</td>
                    <td><span class="badge-id">#${escapeHtml(String(billId))}</span></td>
                    <td>
                        <span class="badge-status ${statusInfo.badgeClass}">${statusInfo.badgeText}</span>
                        ${statusInfo.details ? `<div class="text-muted" style="font-size: 0.75rem; margin-top: 0.25rem;">${statusInfo.details}</div>` : ""}
                    </td>
                `;
                paymentsTableBody.appendChild(tr);
            });

            paymentsTableWrapper.hidden = false;
        }
    }

    /**
     * جلب سجلات المدفوعات من الـ API الحقيقي (/api/Payments) وعرضها في الجدول
     * يدعم فحص التخزين المؤقت وحالات التحميل والقائمة الفارغة والخطأ
     */
    async function loadPayments(force = false) {
        if (isPaymentsLoading) return;
        if (paymentsLoaded && !force) {
            renderPaymentsTable();
            return;
        }

        // تفعيل حالة التحميل وإخفاء باقي الحالات
        paymentsLoading.hidden = false;
        paymentsError.hidden = true;
        paymentsEmpty.hidden = true;
        paymentsTableWrapper.hidden = true;
        isPaymentsLoading = true;

        try {
            const response = await authFetch("/api/Payments");

            if (response.ok) {
                const data = await response.json();
                cachedPayments = Array.isArray(data) ? data : [];
                paymentsLoaded = true;
                paymentsLoading.hidden = true;

                renderPaymentsTable();
            } else {
                paymentsLoading.hidden = true;
                paymentsError.hidden = false;
                if (paymentsErrorMessage) {
                    paymentsErrorMessage.textContent = `حدث خطأ أثناء جلب المدفوعات من الخادم (رمز الاستجابة: ${response.status})`;
                }
            }
        } catch (error) {
            paymentsLoading.hidden = true;
            paymentsError.hidden = false;
            if (paymentsErrorMessage) {
                paymentsErrorMessage.textContent = "تعذر الاتصال بالخادم لجلب المدفوعات. يرجى التأكد من تشغيل الخدمة والمحاولة لاحقاً.";
            }
        } finally {
            isPaymentsLoading = false;
        }
    }

    /**
     * التنقل السلس بين أقسام لوحة التحكم دون إعادة تحميل الصفحة
     */
    function switchSection(sectionId) {
        navItems.forEach((item) => {
            if (item.dataset.section === sectionId) {
                item.classList.add("active");
                const label = item.querySelector(".nav-label")?.textContent;
                if (label && topbarTitle) {
                    topbarTitle.textContent = label;
                }
            } else {
                item.classList.remove("active");
            }
        });

        sections.forEach((sec) => {
            if (sec.id === `section-${sectionId}`) {
                sec.hidden = false;
            } else {
                sec.hidden = true;
            }
        });

        // تحميل البيانات عند فتح القسم المقابل إن لم تكن محملة
        if (sectionId === "customers") {
            loadCustomers(false);
        } else if (sectionId === "plans") {
            loadServicePlans(false);
        } else if (sectionId === "subscriptions") {
            loadSubscriptions(false);
        } else if (sectionId === "bills") {
            loadBills(false);
        } else if (sectionId === "payments") {
            loadPayments(false);
        }

        // إغلاق القائمة الجانبية على الشاشات الصغيرة بعد الاختيار
        if (sidebar && sidebar.classList.contains("open")) {
            sidebar.classList.remove("open");
        }
    }

    function logout(message, messageType = "info") {
        sessionStorage.removeItem(TOKEN_STORAGE_KEY);
        customersLoaded = false;
        plansLoaded = false;
        subscriptionsLoaded = false;
        billsLoaded = false;
        paymentsLoaded = false;
        cachedPayments = [];
        lastLoadedBillCustomerId = null;
        usernameInput.value = "";
        passwordInput.value = "";
        showLoginView();

        if (message) {
            showAlert(message, messageType);
        } else {
            hideAlert();
        }
    }

    // إتاحة دالة الخروج للاستخدام في authFetch
    window.handleLogout = logout;

    /**
     * استعادة الجلسة عند تحميل أو تحديث الصفحة
     */
    function restoreSession() {
        const storedToken = sessionStorage.getItem(TOKEN_STORAGE_KEY)?.trim();
        if (!storedToken) {
            showLoginView();
            return;
        }

        if (isTokenExpired(storedToken)) {
            logout("انتهت صلاحية الجلسة، يرجى تسجيل الدخول مجدداً.", "error");
            return;
        }

        showDashboardView(storedToken);
    }

    // ربط التنقل بالنقر على عناصر القائمة الجانبية
    navItems.forEach((item) => {
        item.addEventListener("click", (e) => {
            e.preventDefault();
            const section = item.dataset.section;
            if (section) {
                switchSection(section);
            }
        });
    });

    // أزرار تحديث بيانات العملاء وإعادة المحاولة
    refreshCustomersBtn?.addEventListener("click", () => {
        loadCustomers(true);
    });

    retryCustomersBtn?.addEventListener("click", () => {
        loadCustomers(true);
    });

    // أزرار تحديث بيانات باقات الإنترنت وإعادة المحاولة
    refreshPlansBtn?.addEventListener("click", () => {
        loadServicePlans(true);
    });

    retryPlansBtn?.addEventListener("click", () => {
        loadServicePlans(true);
    });

    // أزرار تحديث بيانات الاشتراكات وإعادة المحاولة
    refreshSubscriptionsBtn?.addEventListener("click", () => {
        loadSubscriptions(true);
    });

    retrySubscriptionsBtn?.addEventListener("click", () => {
        loadSubscriptions(true);
    });

    // أزرار تحديث بيانات الفواتير وإعادة المحاولة
    refreshBillsBtn?.addEventListener("click", () => {
        loadBills(true);
    });

    retryBillsBtn?.addEventListener("click", () => {
        loadBills(true);
    });

    billCustomerIdInput?.addEventListener("keydown", (e) => {
        if (e.key === "Enter") {
            e.preventDefault();
            loadBills(true);
        }
    });

    // أزرار تحديث بيانات المدفوعات وإعادة المحاولة وتصفية الفاتورة
    refreshPaymentsBtn?.addEventListener("click", () => {
        loadPayments(true);
    });

    retryPaymentsBtn?.addEventListener("click", () => {
        loadPayments(true);
    });

    paymentBillFilterInput?.addEventListener("input", () => {
        if (paymentsLoaded) {
            renderPaymentsTable();
        }
    });

    paymentBillFilterInput?.addEventListener("keydown", (e) => {
        if (e.key === "Enter") {
            e.preventDefault();
            if (!paymentsLoaded) {
                loadPayments(false);
            } else {
                renderPaymentsTable();
            }
        }
    });

    // تبديل ظهور القائمة الجانبية على الشاشات المتنقلة
    sidebarToggle?.addEventListener("click", () => {
        sidebar.classList.toggle("open");
    });

    // معالجة نموذج تسجيل الدخول
    loginForm.addEventListener("submit", async (event) => {
        event.preventDefault();
        hideAlert();

        const username = usernameInput.value.trim();
        const password = passwordInput.value;

        if (!username || !password) {
            showAlert("يرجى إدخال اسم المستخدم وكلمة المرور", "error");
            return;
        }

        setLoading(true);

        try {
            const response = await fetch("/api/account/login", {
                method: "POST",
                headers: {
                    "Content-Type": "application/json"
                },
                body: JSON.stringify({
                    Username: username,
                    Password: password
                })
            });

            if (response.ok) {
                const rawToken = await response.text();
                const token = rawToken.trim();

                sessionStorage.setItem(TOKEN_STORAGE_KEY, token);
                showDashboardView(token);
                hideAlert();
            } else if (response.status === 401) {
                showAlert("اسم المستخدم أو كلمة المرور غير صحيحة", "error");
            } else {
                showAlert("حدث خطأ أثناء تسجيل الدخول، يرجى المحاولة لاحقاً", "error");
            }
        } catch (error) {
            showAlert("تعذر الاتصال بالخادم، يرجى التأكد من تشغيل الخدمة والمحاولة لاحقاً", "error");
        } finally {
            setLoading(false);
        }
    });

    // معالجة تسجيل الخروج
    logoutBtn.addEventListener("click", () => {
        logout("تم تسجيل الخروج بنجاح.", "success");
    });

    // استعادة الجلسة الحالية
    restoreSession();
});
