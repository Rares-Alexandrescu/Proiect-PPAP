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
import { AdaugaFurnizorComponent } from './admin/furnizor/adauga-furnizor/adauga-furnizor';
import { VeziFurnizoriComponent } from './admin/furnizor/vezi-furnizorii/vezi-furnizorii';
import { EditFurnizorComponent } from './admin/furnizor/edit-furnizor/edit-furnizor';
import { VeziPieseFurnizor } from './admin/furnizor/vezi-piese-furnizor/vezi-piese-furnizor';
import { SeteazaPretPiesaFurnizor } from './admin/furnizor/seteaza-pret-piesa-furnizor/seteaza-pret-piesa-furnizor';

import { VeziCompanieComponent } from './admin-local/companie/vezi-companie/vezi-companie';
import { AdaugaAngajatComponent } from './admin-local/companie/adauga-angajat/adauga-angajat';
import { VeziPieseComponent } from './admin-local/furnizor/vezi-piese/vezi-piese';
import { AdaugaPiesaComponent } from './admin-local/furnizor/adauga-piese/adauga-piese';
import { EditPiesaComponent } from './admin-local/furnizor/edit-piese/edit-piese';


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
      },
      {
        path: 'adauga-furnizor',
        component: AdaugaFurnizorComponent 
      },
      {
        path: 'edit-furnizor/:id',
        component: EditFurnizorComponent
      },
      {
        path: 'vezi-furnizorii',
        component: VeziFurnizoriComponent
      },
      {
        path: 'vezi-piese-furnizor/:id',
        component: VeziPieseFurnizor
      },
      {
        path: 'seteaza-pret-piesa-furnizor/:furnizorId/:piesaId',
        component: SeteazaPretPiesaFurnizor
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

  {
    path: 'admin-furnizor',
    children: [
      {
        path: 'vezi-piese',
        component: VeziPieseComponent
      },
      {
        path: 'adauga-piesa',
        component: AdaugaPiesaComponent
      },
      {
        path: 'edit-piesa/:id',
        component: EditPiesaComponent
      }
    ]
  },

  { path: '**', redirectTo: 'dashboard' }
]
