import { Configurador } from './components/configurador/configurador';
import { Routes } from '@angular/router';
import { Catalogo } from './components/catalogo/catalogo';

export const routes: Routes = [
    { path: '', component: Catalogo },
    { path: 'configurador/:id', loadComponent: () => import('./components/configurador/configurador').then(c => c.Configurador) }, // temporalmente apunta aquí
    { path: 'habitacion', loadComponent: () => import('./components/catalogo/catalogo').then(c => c.Catalogo) }, // temporalmente apunta aquí
    { path: '**', redirectTo: '' }
];