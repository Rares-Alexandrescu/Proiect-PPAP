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
import { VeziPieseFurnizorComponent } from './admin/furnizor/vezi-piese-furnizor/vezi-piese-furnizor';
import { SeteazaPretPiesaFurnizorComponent } from './admin/furnizor/seteaza-pret-piesa-furnizor/seteaza-pret-piesa-furnizor';

import { VeziLogisticaIesireDetaliatComponent } from './admin/logistica/vezi-logistica-iesire-detaliat/vezi-logistica-iesire-detaliat';
import { VeziLogisticaIesireComponent } from './admin/logistica/vezi-logistica-iesire/vezi-logistica-iesire';
import { VeziLogisticaIntrareDetaliatComponent } from './admin/logistica/vezi-logistica-intrare-detaliat/vezi-logistica-intrare-detaliat';
import { VeziLogisticaIntrareComponent } from './admin/logistica/vezi-logistica-intrare/vezi-logistica-intrare';

import { VeziCompanieComponent } from './admin-local/companie/vezi-companie/vezi-companie';
import { AdaugaAngajatComponent } from './admin-local/companie/adauga-angajat/adauga-angajat';
import { VeziPieseComponent } from './admin-local/furnizor/vezi-piese/vezi-piese';
import { AdaugaPiesaComponent } from './admin-local/furnizor/adauga-piese/adauga-piese';
import { EditPiesaComponent } from './admin-local/furnizor/edit-piese/edit-piese';
import { VeziFacturaComponent } from './admin-local/furnizor/vezi-factura/vezi-factura';
import { VeziFacturiComponent } from './admin-local/furnizor/vezi-facturi/vezi-facturi';



import { NouaComandaComponent } from './companie/noua-comanda/noua-comanda';
import { ModificaComandaComponent } from './companie/modifica-comanda/modifica-comanda';
import { ComenziCurenteComponent } from './companie/comenzi-curente/comenzi-curente';
import { AdaugaPiesaComandaComponent } from './companie/adauga-piesa/adauga-piesa';
import { VeziComandaComponent } from './companie/vezi-comanda/vezi-comanda';


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
        component: VeziPieseFurnizorComponent
      },
      {
        path: 'seteaza-pret-piesa-furnizor/:furnizorId/:piesaId',
        component: SeteazaPretPiesaFurnizorComponent
      },
      {
        path: 'vezi-logistica-intrare',
        component: VeziLogisticaIntrareComponent
      },
      {
        path: 'vezi-logistica-intrare-detaliat/:facturaId',
        component: VeziLogisticaIntrareDetaliatComponent
      },
      {
        path: 'vezi-logistica-iesire',
        component: VeziLogisticaIesireComponent
      },
      {
        path: 'vezi-logistica-iesire-detliat/:comandaId',
        component: VeziLogisticaIesireDetaliatComponent
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
      },
      {
        path: 'vezi-facturi',
        component: VeziFacturiComponent
      },
      {
        path: 'vezi-factura/:idFactura',
        component: VeziFacturaComponent
      }
    ]
  },
  {
    path: 'compania-ta',
    children: [
      {
        path: 'comenzi-curente',
        component: ComenziCurenteComponent
      },
      {
        path: 'vezi-comanda/:idComanda',
        component: VeziComandaComponent
      },
      {
        path: 'noua-comanda',
        component: NouaComandaComponent
      },
      {
        path: 'modifica-comanda/:idComanda/:idComandaPiesa',
        component: ModificaComandaComponent
      },
      {
        path: 'adauga-piesa/:idFurnizor/:idPiesa',
        component: AdaugaPiesaComandaComponent
      }
    ]
  },

  { path: '**', redirectTo: 'dashboard' }
]
