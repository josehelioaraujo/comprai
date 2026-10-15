export interface AdminSummary {
  totalOrders: number
  totalRevenue: number
  totalCustomers: number
  pendingOrders: number
  cancelledOrders: number
  avgOrderValue: number
}

export interface AdminOrder {
  orderId: string
  status: string
  total: number
  customerName: string | null
  customerEmail: string | null
  paymentMethod: string | null
  itemCount: number
  createdAt: string
}

export interface AdminOrdersPage {
  items: AdminOrder[]
  total: number
  page: number
  pageSize: number
  totalPages: number
}

export interface AdminOrderDetail {
  orderId: string
  status: string
  total: number
  customerName: string | null
  customerEmail: string | null
  customerPhone: string | null
  trackingCode: string | null
  cancelReason: string | null
  paymentMethod: string | null
  paymentStatus: string | null
  paymentAmount: number | null
  shippingZip: string | null
  shippingStreet: string | null
  shippingNumber: string | null
  shippingCity: string | null
  shippingState: string | null
  createdAt: string
  items: AdminOrderItem[]
}

export interface AdminOrderItem {
  productId: string
  productTitle: string
  sku: string | null
  quantity: number
  unitPrice: number
  imageUrl: string | null
}

export interface AdminPaymentsStats {
  totalPayments: number
  totalConfirmed: number
  totalPending: number
  totalFailed: number
  pixCount: number
  creditCardCount: number
  debitCardCount: number
}

export interface AdminCustomersStats {
  totalCustomers: number
  verifiedCustomers: number
  withTotpCustomers: number
  newThisMonth: number
}

export interface AdminSalesChartPoint {
  date: string
  revenue: number
  orders: number
}

export interface AdminTopProduct {
  productId: string
  productTitle: string
  imageUrl: string | null
  totalQuantity: number
  totalRevenue: number
}
