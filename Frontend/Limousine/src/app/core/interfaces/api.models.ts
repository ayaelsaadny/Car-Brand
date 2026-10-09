export interface ApiResponse<T = any> {
  data: T;
  statusCode: number;
  message: string;
}

export interface VehicleImage {
  id: number;
  vehicleId: number;
  imageUrl: string;
  isPrimary: boolean;
  isMain?: boolean;
  displayOrder: number;
  createdAt?: string;
}

export interface Vehicle {
  id: number;
  name: string;
  description?: string;
  make: string;
  model: string;
  year: number;
  licensePlate: string;
  color?: string;
  passengerCapacity: number;
  luggageCapacity: number;
  pricePerHour: number;
  pricePerKm?: number;
  features?: string;
  isActive: boolean;
  isDefault: boolean;
  createdAt?: string;
  updatedAt?: string;
  images: VehicleImage[];
}

export interface CreateVehicleRequest {
  name: string;
  description?: string;
  make: string;
  model: string;
  year: number;
  licensePlate: string;
  color?: string;
  passengerCapacity: number;
  luggageCapacity: number;
  pricePerHour: number;
  pricePerKm?: number;
  features?: string;
  isActive: boolean;
  isDefault: boolean;
}

export interface UpdateVehicleRequest {
  name: string;
  description?: string;
  make: string;
  model: string;
  year: number;
  licensePlate: string;
  color?: string;
  passengerCapacity: number;
  luggageCapacity: number;
  pricePerHour: number;
  pricePerKm?: number;
  features?: string;
  isActive: boolean;
  isDefault: boolean;
}

export interface UpdateVehicleStatusRequest {
  isActive: boolean;
}

export interface ServiceItem {
  id: number;
  categoryId: number;
  categoryName: string;
  name: string;
  description?: string;
  price?: number;
  imageUrl?: string;
  image?: string;
  fromLocation?: string;
  toLocation?: string;
  isActive: boolean;
  allowVehicleSelection: boolean;
  displayOrder: number;
  createdAt?: string;
  updatedAt?: string;
  totalBookings?: number;
}

export interface CreateServiceRequest {
  name: string;
  description?: string;
  categoryId: number;
  price?: number;
  imageUrl?: string;
  fromLocation?: string;
  toLocation?: string;
  isActive: boolean;
  allowVehicleSelection: boolean;
  displayOrder: number;
  translations?: Record<string, EntityTranslation>;
}

export interface UpdateServiceRequest {
  name: string;
  description?: string;
  categoryId: number;
  price?: number;
  imageUrl?: string;
  fromLocation?: string;
  toLocation?: string;
  isActive: boolean;
  allowVehicleSelection: boolean;
  displayOrder: number;
  translations?: Record<string, EntityTranslation>;
}

export interface UpdateServiceStatusRequest {
  isActive: boolean;
}

export interface UpdateServiceDisplayOrderRequest {
  displayOrder: number;
}

export interface ServiceFilterQuery {
  categoryId?: number;
  category?: string;
  search?: string;
  minPrice?: number;
  maxPrice?: number;
  allowVehicleSelection?: boolean;
  sortBy?: string;
  activeOnly?: boolean;
}

export interface EntityTranslation {
  name: string;
  description?: string;
}

export interface Category {
  id: number;
  name: string;
  description?: string;
  imageUrl?: string;
  image?: string;
  isActive: boolean;
  displayOrder: number;
  servicesCount?: number;
  createdAt?: string;
  updatedAt?: string;
}

export interface CreateCategoryRequest {
  name: string;
  description?: string;
  imageUrl?: string;
  isActive: boolean;
  displayOrder: number;
  translations?: Record<string, EntityTranslation>;
}

export interface UpdateCategoryRequest {
  name: string;
  description?: string;
  imageUrl?: string;
  isActive: boolean;
  displayOrder: number;
  translations?: Record<string, EntityTranslation>;
}

export interface UpdateCategoryStatusRequest {
  isActive: boolean;
}

export interface UpdateCategoryDisplayOrderRequest {
  displayOrder: number;
}

export interface WebsiteSetting {
  id: number;
  key: string;
  value: string;
  description?: string;
  group?: string;
}

export type RoleType = 'Admin' | 'Employee' | 'Customer';

export interface User {
  id: number;
  fullName: string;
  email: string;
  role: RoleType | string;
  phoneNumber?: string;
  token?: string;
}

export interface AuthResponse {
  token: string;
  userId?: number;
  fullName?: string;
  email?: string;
  role?: string;
  user?: User;
  expiresAt?: string;
}

export interface CustomerProfile {
  customerId: number;
  userId: number;
  fullName: string;
  email: string;
  phoneNumber: string;
  firstName: string;
  lastName: string;
  address?: string;
  role: string;
  createdAt: string;
  totalBookings: number;
}

export interface UpdateProfileRequest {
  fullName?: string;
  firstName?: string;
  lastName?: string;
  phoneNumber: string;
  address?: string;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
  confirmNewPassword: string;
}

export interface UserItem {
  id: number;
  fullName: string;
  email: string;
  phoneNumber?: string;
  role: string;
  roleName?: string;
  isActive: boolean;
  createdAt: string;
}

export interface ToggleUserStatusRequest {
  isActive: boolean;
}

export type BookingStatusType = 'Pending' | 'Confirmed' | 'InProgress' | 'Completed' | 'Cancelled' | 'Rejected';

export interface BookingStatusHistory {
  id: number;
  status: number | string;
  statusName: string;
  notes?: string;
  changedBy?: string;
  createdAt: string;
}

export interface BookingRequestItem {
  id: number;
  bookingReference: string;
  customerId?: number;
  customerName: string;
  customerEmail: string;
  customerPhone: string;
  serviceId: number;
  serviceName: string;
  categoryName?: string;
  vehicleId?: number;
  vehicleName?: string;
  vehicleLicensePlate?: string;
  pickupLocation: string;
  dropoffLocation: string;
  pickupDateTime: string;
  pickupDate?: string;
  pickupTime?: string;
  returnDateTime?: string;
  flightNumber?: string;
  numberOfPassengers: number;
  numberOfLuggage: number;
  specialNotes?: string;
  customerNotes?: string;
  estimatedPrice?: number;
  status: number | string;
  statusName: string;
  rejectionReason?: string;
  cancellationReason?: string;
  createdAt: string;
  updatedAt?: string;
  statusHistories?: BookingStatusHistory[];
}

export interface PagedResult<T> {
  items: T[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export interface BookingSearchFilter {
  search?: string;
  customerName?: string;
  customerEmail?: string;
  customerPhone?: string;
  bookingReference?: string;
  status?: number | string;
  serviceId?: number;
  vehicleId?: number;
  date?: string;
  fromDate?: string;
  toDate?: string;
  pageNumber?: number;
  pageSize?: number;
}

export interface UpdateBookingStatusRequest {
  status: number | string;
  notes?: string;
}

export interface AcceptBookingRequest {
  notes?: string;
}

export interface RejectBookingRequest {
  rejectionReason?: string;
  reason?: string;
}

export interface CancelBookingRequest {
  cancellationReason?: string;
  reason?: string;
}

export interface CompleteBookingRequest {
  notes?: string;
}
