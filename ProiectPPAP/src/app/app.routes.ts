import { Routes } from '@angular/router';
import { LoginComponent } from './login/login';
import { RegisterComponent } from './register/register';
import { ResetareParolaComponent } from './resetare-parola/resetare-parola';
import { EditAccountComponent } from './edit-account/edit-account';
import { DashboardComponent } from './dashboard/dashboard';
export const routes: Routes = [
  { path: 'login', component: LoginComponent },
  { path: 'register', component: RegisterComponent },
  { path: 'resetare-parola', component: ResetareParolaComponent },
  { path: 'edit-account', component: EditAccountComponent },
{ path: 'dashboard', component: DashboardComponent}]
