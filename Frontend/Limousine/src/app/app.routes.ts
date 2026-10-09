import { Routes } from '@angular/router';
import { HomeComponent } from './feature/home/home.component';
import { MainLayoutComponent } from './shared/components/main-layout/main-layout';
import { DashboardComponent } from './feature/dashboard/dashboard.component';
import { VehiclesListComponent } from './feature/vehicles/vehicles-list/vehicles-list.component';
import { BookingsListComponent } from './feature/bookings/bookings-list/bookings-list.component';
import { ServicesListComponent } from './feature/services/services-list/services-list.component';
import { CategoriesListComponent } from './feature/categories/categories-list/categories-list.component';
import { SettingsListComponent } from './feature/settings/settings-list/settings-list.component';
import { LoginComponent } from './feature/auth/login/login.component';
import { RegisterComponent } from './feature/auth/register/register.component';
import { ProfileComponent } from './feature/account/profile/profile.component';
import { ChangePasswordComponent } from './feature/account/change-password/change-password.component';
import { CustomerBookingsComponent } from './feature/customer/bookings/customer-bookings.component';
import { CustomerProfileComponent } from './feature/customer/profile/customer-profile.component';
import { UsersListComponent } from './feature/users/users-list/users-list.component';
import { langGuard } from './core/guards/lang.guard';
import { authGuard } from './core/guards/auth.guard';
import { staffGuard } from './core/guards/staff.guard';
import { adminGuard } from './core/guards/admin.guard';

export const routes: Routes = [
  // 1. Language prefixed routes: /en, /fr
  {
    path: ':lang',
    canActivate: [langGuard],
    children: [
      // Landing page (Public APIs & booking)
      {
        path: '',
        component: HomeComponent
      },

      // Auth routes (Dedicated public auth pages)
      {
        path: 'auth',
        children: [
          { path: 'login', component: LoginComponent },
          { path: 'register', component: RegisterComponent },
          { path: '', redirectTo: 'login', pathMatch: 'full' }
        ]
      },

      // Portal in MainLayout
      {
        path: '',
        component: MainLayoutComponent,
        children: [
          // Staff-accessible operations (Admin + Employee)
          { path: 'dashboard', component: DashboardComponent, canActivate: [staffGuard] },
          { path: 'vehicles', component: VehiclesListComponent, canActivate: [staffGuard] },
          { path: 'bookings', component: BookingsListComponent, canActivate: [staffGuard] },
          { path: 'services', component: ServicesListComponent, canActivate: [staffGuard] },
          { path: 'categories', component: CategoriesListComponent, canActivate: [staffGuard] },

          // Admin-only management
          { path: 'users', component: UsersListComponent, canActivate: [adminGuard] },
          { path: 'settings', component: SettingsListComponent, canActivate: [adminGuard] },

          // Customer VIP Portal
          {
            path: 'customer',
            canActivate: [authGuard],
            children: [
              { path: 'bookings', component: CustomerBookingsComponent },
              { path: 'profile', component: CustomerProfileComponent },
              { path: '', redirectTo: 'bookings', pathMatch: 'full' }
            ]
          },

          // Account & Profile (All authenticated roles)
          {
            path: 'account',
            canActivate: [authGuard],
            children: [
              { path: 'profile', component: ProfileComponent },
              { path: 'change-password', component: ChangePasswordComponent },
              { path: '', redirectTo: 'profile', pathMatch: 'full' }
            ]
          }
        ]
      }
    ]
  },

  // 2. Default redirect from root to /en
  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'en'
  },

  // 3. Fallback for any unknown route
  {
    path: '**',
    redirectTo: 'en'
  }
];
