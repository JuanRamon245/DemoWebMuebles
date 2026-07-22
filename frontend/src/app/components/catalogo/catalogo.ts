import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { Mueble } from '../../interfaces/mueble'; // Asegúrate de que la ruta a tu interfaz sea correcta

@Component({
  selector: 'app-catalogo',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './catalogo.html',
  styleUrl: './catalogo.scss',
})
export class Catalogo implements OnInit {
  // Base de datos de prueba adaptada a la nueva interfaz polimórfica
  mueblesOriginales: Mueble[] = [
    {
      id: 'P001',
      nombre: 'Puerta Modular Paramétrica',
      categoria: 'puerta',
      precioBase: 149.99,
      imagenes: [
        'https://images.unsplash.com/photo-1517646287270-a5a9ca602e5c?q=80&w=400',
        'https://images.unsplash.com/photo-1513694203232-719a280e022f?q=80&w=400',
        'https://images.unsplash.com/photo-1534438097545-a8ea4922dcaf?q=80&w=400'
      ],
      descripcion: 'Puerta de madera maciza totalmente adaptable al marco de tu obra.',
      atributosPorDefecto: [
        { clave: 'Alto', valor: '2.1m' },
        { clave: 'Ancho', valor: '0.8m' }
      ]
    },
    {
      id: 'V001',
      nombre: 'Ventana individual Climalit',
      categoria: 'ventana',
      precioBase: 189.50,
      imagenes: [
        'https://images.unsplash.com/photo-1509644851169-2acc08aa25b5?q=80&w=400',
        'https://images.unsplash.com/photo-1540518614846-7eded433c457?q=80&w=400',
        'https://images.unsplash.com/photo-1600585154340-be6161a56a0c?q=80&w=400'
      ],
      descripcion: 'Aislamiento térmico y acústico con marco personalizable de PVC o madera.',
      atributosPorDefecto: [
        { clave: 'Alto', valor: '1.2m' },
        { clave: 'Ancho', valor: '1.2m' }
      ]
    },
    {
      id: 'E001',
      nombre: 'Estantería de Salón Proporcional',
      categoria: 'estanteria',
      precioBase: 89.99,
      imagenes: [
        'https://images.unsplash.com/photo-1540518614846-7eded433c457?q=80&w=400',
        'https://images.unsplash.com/photo-1595428774223-ef52624120d2?q=80&w=400',
        'https://images.unsplash.com/photo-1600585154340-be6161a56a0c?q=80&w=400'
      ],
      descripcion: 'Estantería con baldas inteligentes que respetan el límite de seguridad de 30cm.',
      atributosPorDefecto: [
        { clave: 'Alto', valor: '2.0m' },
        { clave: 'Ancho', valor: '1.0m' },
        { clave: 'Profundidad', valor: '0.4m' }
      ]
    },
    {
      id: 'M001',
      nombre: 'Mesa de Comedor Roble',
      categoria: 'mesa',
      precioBase: 245.00,
      imagenes: [
        'https://images.unsplash.com/photo-1577140917170-285929fb55b7?q=80&w=400',
        'https://images.unsplash.com/photo-1509644851169-2acc08aa25b5?q=80&w=400',
        'https://images.unsplash.com/photo-1517646287270-a5a9ca602e5c?q=80&w=400'
      ],
      descripcion: 'Mesa robusta ideal para comedores y zonas de reunión amplias.',
      atributosPorDefecto: [
        { clave: 'Alto', valor: '0.75m' },
        { clave: 'Ancho', valor: '1.6m' },
        { clave: 'Profundidad', valor: '0.9m' }
      ]
    }
  ];

  mueblesFiltrados: Mueble[] = [];
  textoBusqueda: string = '';
  categoriaSeleccionada: string = 'todos';

  ngOnInit() {
    this.destruirInstanciaUnityResidual();
    this.mueblesFiltrados = [...this.mueblesOriginales];
  }

  private destruirInstanciaUnityResidual() {
    const instance = (window as any).unityInstance;
    if (instance) {
      try {
        // Usamos el método real de GestorMuebles
        instance.SendMessage('GestorMuebles', 'LimpiarMemoriaWebGL');
      } catch (e) { }

      try {
        instance.Quit().then(() => {
          (window as any).unityInstance = null;
          this.limpiarDOMScripts();
        }).catch(() => {
          (window as any).unityInstance = null;
          this.limpiarDOMScripts();
        });
      } catch (e) {
        (window as any).unityInstance = null;
        this.limpiarDOMScripts();
      }
    }
  }

  private limpiarDOMScripts(): void {
    // Busca y elimina del DOM los scripts cargados dinámicamente por Unity
    const scripts = Array.from(document.querySelectorAll('script'));
    scripts.forEach((script) => {
      if (
        script.src.includes('.loader.js') ||
        script.src.includes('.framework.js') ||
        script.src.includes('Build/')
      ) {
        script.remove();
      }
    });
  }

  filtrarMuebles() {
    this.mueblesFiltrados = this.mueblesOriginales.filter(mueble => {
      const coincideBusqueda = mueble.nombre.toLowerCase().includes(this.textoBusqueda.toLowerCase()) ||
        mueble.descripcion.toLowerCase().includes(this.textoBusqueda.toLowerCase());

      const coincideCategoria = this.categoriaSeleccionada === 'todos' || mueble.categoria === this.categoriaSeleccionada;

      return coincideBusqueda && coincideCategoria;
    });
  }

  seleccionarCategoria(cat: string) {
    this.categoriaSeleccionada = cat;
    this.filtrarMuebles();
  }
}