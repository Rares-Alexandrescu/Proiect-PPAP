import { Routes } from '@angular/router';
import { LoginComponent } from './login/login';
import { RegisterComponent } from './register/register';
import { ResetareParolaComponent } from './resetare-parola/resetare-parola';
import { EditAccountComponent } from './edit-account/edit-account';
import { DashboardComponent } from './dashboard/dashboard';
import { AdminDashboardComponent } from './admin/admin-dashboard/admin-dashboard';
import { AdaugaCompanieComponent,  } from './admin/companie/add-companie/add-companie';
import { EditCompanieComponent } from './admin/companie/edit-companie/edit-companie';
import { VeziCompaniiComponent } from './admin/companie/vezi-companii/vezi-companii';
import { VeziCompanieComponent } from './admin-local/companie/vezi-companie/vezi-companie';
import { AdaugaAngajatComponent } from './admin-local/companie/adauga-angajat/adauga-angajat';
export const routes: Routes = [
  { path: 'login', component: LoginComponent },
  { path: 'register', component: RegisterComponent },
  { path: 'resetare-parola', component: ResetareParolaComponent },
  { path: 'edit-account', component: EditAccountComponent },
  { path: 'dashboard', component: DashboardComponent },

  {
    path: 'admin',
    children: [
      {
        path: '',
        component: AdminDashboardComponent
      },
      {
        path: 'vezi-companii',
        component: VeziCompaniiComponent
      },
      {
        path: 'adauga-companie',
        component: AdaugaCompanieComponent
      },
      {
        path: 'edit-companie/:id',
        component: EditCompanieComponent
      }
    ]
  },

  {
    path: 'admin-companie',
    children: [
      {
        path: 'vezi-companie',
        component: VeziCompanieComponent
      },
      {
        path: 'adauga-angajat',
        component: AdaugaAngajatComponent
      }
    ]
  },


  { path: '**', redirectTo: 'dashboard' }
]
