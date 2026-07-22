import { Component, ElementRef, OnInit, ViewChild, OnDestroy, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { Mueble, ConfigPuerta, ConfigEstanteria, ConfigVentana } from '../../interfaces/mueble';

export interface OpcionSelect {
  value: string;
  label: string;
  extra?: string;
}

export interface EsquemaCampo {
  clave: string;
  etiqueta: string;
  tipo: 'range' | 'select' | 'checkbox';
  unidad?: string;
  min?: number;
  max?: number;
  step?: number;
  opciones?: OpcionSelect[];
  soloSiClave?: string;
  soloSiValorEsperado?: any;
}

@Component({
  selector: 'app-configurador',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './configurador.html',
  styleUrls: ['./configurador.scss']
})
export class Configurador implements OnInit, OnDestroy {

  @ViewChild('unityIframe') unityIframe!: ElementRef<HTMLIFrameElement>;

  // ---- ESTADO DE UNITY ----
  unityCargado: boolean = false;
  mostrarLoader: boolean = true;
  progresoCarga: number = 0;
  private colaMensajesUnity: any[] = []; // El buzón de espera

  // ---- MODELO DINÁMICO ----
  producto!: Mueble;
  config!: any;
  esquemaConfig: EsquemaCampo[] = [];
  limiteMaxBaldasActual: number = 10;
  private readonly GROSOR_ESTRUCTURA_ESTANTERIA = 0.03;

  // ---- ESTADO DE LA VISTA ----
  modoAmedida: boolean = false;
  cantidad: number = 1;
  imagenActual: string = '';

  // ---- VARIABLES DE COSTE ----
  precioTotal: number = 0;
  camara = { x: 45, y: 180, z: 0 };

  private readonly metodosDeCamara = new Set(['ActualizarCamara', 'ActivarModoEdicion']);

  constructor(private route: ActivatedRoute) { }

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      const id = params.get('id') || 'P001';

      this.cargarProductoMock(id);
      this.inicializarConfiguracion();
      this.construirEsquemaConfig();
      this.calcularPrecio();
    });
  }

  ngOnDestroy(): void {
    this.unityCargado = false;
    this.mostrarLoader = true;
    this.progresoCarga = 0;
    this.colaMensajesUnity = [];
  }

  // ---- COMUNICACIÓN CON UNITY (IFRAME) ----

  @HostListener('window:message', ['$event'])
  onMessage(event: MessageEvent) {
    if (!event.data) return;

    // Actualizamos la barra de progreso
    if (event.data.type === 'UNITY_PROGRESO') {
      this.progresoCarga = Math.round(event.data.valor * 100);
    }
    // Unity terminó de cargar
    else if (event.data.type === 'UNITY_CARGADO') {
      this.unityCargado = true;
      this.procesarColaUnity(); // Enviamos los datos del mueble inmediatamente

      // Mantenemos la cortina bajada 500ms para tapar el logo de Unity
      setTimeout(() => {
        this.mostrarLoader = false;
      }, 2100);
    }
  }

  // Hemos añadido objetoDestino opcional para poder hablar con "GestorMuebles"
  private enviarAUnity(metodo: string, valor: any, objetoDestino?: string) {
    const destino = objetoDestino ? objetoDestino :
      (this.metodosDeCamara.has(metodo) ? 'CamaraPrincipal' : this.obtenerNombreGameObjectUnity());

    const mensaje = {
      type: 'MANDAR_A_UNITY',
      objeto: destino,
      metodo: metodo,
      valor: valor
    };

    if (!this.unityCargado) {
      this.colaMensajesUnity.push(mensaje);
      return;
    }

    if (this.unityIframe && this.unityIframe.nativeElement.contentWindow) {
      this.unityIframe.nativeElement.contentWindow.postMessage(mensaje, '*');
    }
  }

  private procesarColaUnity() {
    while (this.colaMensajesUnity.length > 0) {
      const msg = this.colaMensajesUnity.shift();
      if (this.unityIframe && this.unityIframe.nativeElement.contentWindow) {
        this.unityIframe.nativeElement.contentWindow.postMessage(msg, '*');
      }
    }
  }

  private obtenerNombreGameObjectUnity(): string {
    switch (this.producto.categoria) {
      case 'estanteria': return 'Estanteria_Parametrica';
      case 'puerta': return 'Puerta_Parametrica';
      case 'ventana': return 'Ventana_Parametrica';
      case 'mesa': return 'Mesa_Parametrica';
      default: return 'MuebleObjetoUnity';
    }
  }

  private sincronizarTodoConUnity() {
    this.enviarAUnity('AplicarConfiguracion', JSON.stringify(this.obtenerConfigParaUnity()));
  }

  private obtenerConfigParaUnity(): any {
    if (this.producto.categoria === 'estanteria') {
      return { ...this.config, numBaldas: this.config.numBaldas + 2 };
    }
    return this.config;
  }

  // ---- CONTROL DE VISTAS ----
  activarModoMedida() {
    this.modoAmedida = true;

    // Al encolar en este orden, nos aseguramos de que cuando Unity avise
    // de que está listo, procesará primero la categoría, luego el modo y luego los datos.
    this.enviarAUnity('MostrarCategoria', this.producto.categoria, 'GestorMuebles');
    this.enviarAUnity('ActivarModoEdicion', 1);

    this.resetearValores(); // Esto encolará la configuración y la cámara
  }

  terminarProducto() {
    this.modoAmedida = false;
    this.unityCargado = false;
    this.mostrarLoader = true; // Volvemos a activar el loader
    this.progresoCarga = 0;    // Reseteamos la barra
    this.colaMensajesUnity = [];
  }

  cambiarImagen(img: string) {
    this.imagenActual = img;
  }

  resetearValores() {
    this.inicializarConfiguracion();
    this.construirEsquemaConfig();
    this.resetearCamara();
    this.sincronizarTodoConUnity();
    this.calcularPrecio();
  }

  // ---- LOGICA DE NEGOCIO Y MODELOS MOCK ----

  alCambiarParametro(parametro: string) {
    if (this.producto.limites && this.producto.limites[parametro]) {
      const rango = this.producto.limites[parametro];
      let valorActual = this.config[parametro];

      let minEfectivo = rango.min;
      let maxEfectivo = rango.max;

      if (parametro === 'numBaldas') {
        minEfectivo = rango.min - 2;
        maxEfectivo = this.limiteMaxBaldasActual - 2;
      }

      if (valorActual < minEfectivo) valorActual = minEfectivo;
      if (valorActual > maxEfectivo) valorActual = maxEfectivo;

      this.config[parametro] = valorActual;
    }

    if (this.producto.categoria === 'estanteria' && this.config.margenSimetrico) {
      if (parametro === 'margenSuperior') this.config.margenInferior = this.config.margenSuperior;
      if (parametro === 'margenInferior') this.config.margenSuperior = this.config.margenInferior;
    }

    if (this.producto.categoria === 'estanteria' &&
      ['alto', 'profundidad', 'tamanoBaldas', 'margenSuperior', 'margenInferior'].includes(parametro)) {
      this.actualizarLimiteBaldas();
    }

    this.calcularPrecio();
    this.enviarAUnity('AplicarConfiguracion', JSON.stringify(this.obtenerConfigParaUnity()));
  }

  private actualizarLimiteBaldas() {
    const alto = this.config.alto;
    const tamanoBaldas = this.config.tamanoBaldas;
    let margenSuperior = this.config.margenSuperior;
    let margenInferior = this.config.margenInferior;
    const grosorEstructura = this.GROSOR_ESTRUCTURA_ESTANTERIA;

    let espacioDisponibleY = alto - (grosorEstructura * 2) - margenSuperior - margenInferior;

    if (espacioDisponibleY < 0.3) {
      const maxMargen = Math.max(0.05, (alto - (grosorEstructura * 2) - 0.3) / 2);
      margenSuperior = Math.min(Math.max(margenSuperior, 0.05), maxMargen);
      margenInferior = Math.min(Math.max(margenInferior, 0.05), maxMargen);
      espacioDisponibleY = alto - (grosorEstructura * 2) - margenSuperior - margenInferior;
    }

    let maxIntermediasPosibles = Math.floor((espacioDisponibleY - 0.3) / (0.3 + tamanoBaldas));
    maxIntermediasPosibles = Math.max(2, maxIntermediasPosibles);

    this.limiteMaxBaldasActual = maxIntermediasPosibles + 2;
    const maxUsuario = this.limiteMaxBaldasActual - 2;

    const campoBaldas = this.esquemaConfig.find(c => c.clave === 'numBaldas');
    if (campoBaldas) campoBaldas.max = maxUsuario;

    if (this.config.numBaldas > maxUsuario) {
      this.config.numBaldas = maxUsuario;
    }
  }

  calcularPrecio() {
    let precio = this.producto.precioBase;

    const materialSeleccionado = this.producto.materiales?.find(m => m.id === this.config.material);
    if (materialSeleccionado) {
      precio += materialSeleccionado.sobreprecio;
    }

    if (this.producto.categoria === 'puerta') {
      const areaBase = 2.1 * 0.9;
      const areaActual = this.config.alto * this.config.ancho;
      if (areaActual > areaBase) precio += (areaActual - areaBase) * 50;
      if (this.config.cerradura) precio += 25;

    } else if (this.producto.categoria === 'ventana') {
      const volumenBase = 2.0 * 1.2 * 0.6;
      const volumenActual = this.config.alto * this.config.ancho;
      const volumenMarcos = (this.config.grosorDelMarco + this.config.perfilDelExterior) * 2.5;
      if (volumenActual > volumenBase) precio += (volumenActual - volumenBase + volumenMarcos) * 60;

    } else if (this.producto.categoria === 'estanteria') {
      const volumenBase = 2.0 * 1.2 * 0.6;
      const volumenActual = this.config.alto * this.config.ancho * this.config.profundidad;
      if (volumenActual > volumenBase) precio += (volumenActual - volumenBase) * 120;

      const numBaldasTotal = this.config.numBaldas + 2;
      precio += numBaldasTotal * 15;
    }

    this.precioTotal = precio;
  }

  anadirAlCarrito(tipo: 'predeterminado' | 'medida') {
    const trackingCarrito = {
      idProducto: this.producto.id,
      nombre: this.producto.nombre,
      categoria: this.producto.categoria,
      precioUnitario: this.precioTotal,
      subtotal: this.precioTotal * this.cantidad,
      cantidad: this.cantidad,
      configuracion: tipo === 'medida' ? { ...this.config } : 'Estándar'
    };
    console.log('Push Carrito:', trackingCarrito);
    alert(`Añadido al carrito con éxito.`);
  }

  // ---- CÁMARA ----
  resetearCamara() {
    this.camara = { x: 45, y: 0, z: 0 };
    this.alCambiarCamara();
  }

  alCambiarCamara() {
    this.enviarAUnity('ActualizarCamara', `${this.camara.x},${this.camara.y},${this.camara.z}`);
  }

  cargarProductoMock(id: string) {
    switch (id) {
      case 'P001':
        this.producto = {
          id: 'P001',
          nombre: 'Puerta Paramétrica Avanzada',
          categoria: 'puerta',
          precioBase: 120.00,
          imagenes: [
            'https://images.unsplash.com/photo-1513694203232-719a280e022f?q=80&w=800',
            'https://images.unsplash.com/photo-1534438097545-a8ea4922dcaf?q=80&w=800',
            'https://images.unsplash.com/photo-1481277542470-605612bd2d61?q=80&w=800'
          ],
          descripcion: 'Esta puerta combina elegancia y resistencia. Fabricada con materiales de alta calidad.',
          atributosPorDefecto: [
            { clave: 'Alto', valor: '2.1m' },
            { clave: 'Ancho', valor: '0.9m' },
            { clave: 'Material', valor: 'Madera de Abedul' },
            { clave: 'Apertura', valor: 'Manillar clásico' }
          ],
          materiales: [
            { id: 'Abedul', nombre: 'Madera de Abedul (Clara)', sobreprecio: 0 },
            { id: 'Cerezo', nombre: 'Madera de Cerezo (Rojiza)', sobreprecio: 0 },
            { id: 'Nogal', nombre: 'Madera de Nogal Oscuro', sobreprecio: 35 },
            { id: 'PVC', nombre: 'Plástico PVC Gris', sobreprecio: 0 }
          ],
          limites: {
            alto: { min: 1.8, max: 2.5, defecto: 2.1 },
            ancho: { min: 0.6, max: 1.3, defecto: 0.9 },
            grosorDelMarco: { min: 0.05, max: 0.2, defecto: 0.1 }
          }
        };
        break;

      case 'V001':
        this.producto = {
          id: 'V001',
          nombre: 'Ventana Individual Climalit',
          categoria: 'ventana',
          precioBase: 189.50,
          imagenes: [
            'https://images.unsplash.com/photo-1509644851169-2acc08aa25b5?q=80&w=800',
            'https://images.unsplash.com/photo-1540518614846-7eded433c457?q=80&w=800'
          ],
          descripcion: 'Aislamiento térmico y acústico con marco personalizable de PVC o madera.',
          atributosPorDefecto: [
            { clave: 'Alto', valor: '1.2m' },
            { clave: 'Ancho', valor: '1.2m' },
            { clave: 'Material', valor: 'Madera de Abedul' }
          ],
          materiales: [
            { id: 'Abedul', nombre: 'Madera de Abedul (Clara)', sobreprecio: 0 },
            { id: 'Cerezo', nombre: 'Madera de Cerezo (Rojiza)', sobreprecio: 15 },
            { id: 'Nogal', nombre: 'Madera de Nogal Oscuro', sobreprecio: 35 },
            { id: 'PVC', nombre: 'Plástico PVC Gris', sobreprecio: 0 }
          ],
          limites: {
            alto: { min: 0.3, max: 3.0, defecto: 1.2 },
            ancho: { min: 0.3, max: 3.0, defecto: 1.2 },
            grosorDelMarco: { min: 0.05, max: 0.3, defecto: 0.1 },
            perfilDelExterior: { min: 0.03, max: 0.15, defecto: 0.06 },
          }
        };
        break;

      case 'E001':
        this.producto = {
          id: 'E001',
          nombre: 'Armario Estantería Modular Máster',
          categoria: 'estanteria',
          precioBase: 210.00,
          imagenes: [
            'https://images.unsplash.com/photo-1595428774223-ef52624120d2?q=80&w=800',
            'https://images.unsplash.com/photo-1540518614846-7eded433c457?q=80&w=800'
          ],
          descripcion: 'Armario de gran almacenamiento configurable en altura y distribución de baldas.',
          atributosPorDefecto: [
            { clave: 'Alto', valor: '2.00m' },
            { clave: 'Ancho', valor: '1.00m' },
            { clave: 'Profundidad', valor: '0.4m' },
            { clave: 'Nº Baldas', valor: '2 unidades' },
            { clave: 'Calidad de la madera', valor: 'Madera de Abedul Estándar' }
          ],
          materiales: [
            { id: 'Abedul', nombre: 'Madera de Abedul Estándar', sobreprecio: 0 },
            { id: 'Cerezo', nombre: 'Madera de Cerezo Premium', sobreprecio: 0 },
            { id: 'Nogal', nombre: 'Madera de Nogal Macizo', sobreprecio: 45 }
          ],
          limites: {
            alto: { min: 1.0, max: 3.0, defecto: 2.0 },
            ancho: { min: 0.6, max: 2.4, defecto: 1.0 },
            profundidad: { min: 0.3, max: 1, defecto: 0.4 },
            tamanoBaldas: { min: 0.015, max: 0.06, defecto: 0.02 },
            grosorEstructura: { min: 0.02, max: 0.08, defecto: 0.03 },
            numBaldas: { min: 4, max: 10, defecto: 4 }
          }
        };
        break;

      case 'M001':
        this.producto = {
          id: 'M001',
          nombre: 'Mesa de Comedor Roble',
          categoria: 'mesa',
          precioBase: 245.00,
          imagenes: [
            'https://images.unsplash.com/photo-1577140917170-285929fb55b7?q=80&w=800'
          ],
          descripcion: 'Mesa robusta ideal para comedores y zonas de reunión amplias.',
          atributosPorDefecto: [
            { clave: 'Alto', valor: '0.75m' },
            { clave: 'Ancho', valor: '1.6m' },
            { clave: 'Profundidad', valor: '0.9m' }
          ],
          limites: {
            alto: { min: 0.6, max: 1.0, defecto: 0.75 },
            ancho: { min: 1.0, max: 2.4, defecto: 1.6 },
            profundidad: { min: 0.6, max: 1.2, defecto: 0.9 }
          }
        };
        break;

      default:
        this.producto = {
          id: 'P001',
          nombre: 'Puerta Paramétrica Avanzada',
          categoria: 'puerta',
          precioBase: 120.00,
          imagenes: [
            'https://images.unsplash.com/photo-1513694203232-719a280e022f?q=80&w=800'
          ],
          descripcion: 'Esta puerta combina elegancia y resistencia. Fabricada con materiales de alta calidad.',
          atributosPorDefecto: [
            { clave: 'Alto', valor: '2.1m' },
            { clave: 'Ancho', valor: '0.9m' }
          ],
          limites: {
            alto: { min: 1.8, max: 2.5, defecto: 2.1 },
            ancho: { min: 0.6, max: 1.3, defecto: 0.9 }
          }
        };
        break;
    }
    this.imagenActual = this.producto.imagenes[0];
  }

  inicializarConfiguracion() {
    const lim = this.producto?.limites;

    if (this.producto.categoria === 'puerta') {
      this.config = {
        alto: lim?.['alto']?.defecto ?? 2.1,
        ancho: lim?.['ancho']?.defecto ?? 0.9,
        grosorDelMarco: lim?.['grosorDelMarco']?.defecto ?? 0.1,
        material: 'Abedul',
        pomo: 'Manillar',
        cerradura: false
      } as ConfigPuerta;

    } else if (this.producto.categoria === 'ventana') {
      this.config = {
        alto: lim?.['alto']?.defecto ?? 1.2,
        ancho: lim?.['ancho']?.defecto ?? 1.2,
        grosorDelMarco: lim?.['grosorDelMarco']?.defecto ?? 0.1,
        perfilDelExterior: lim?.['perfilDelExterior']?.defecto ?? 0.06,
        material: 'Abedul'
      } as ConfigVentana;

    } else if (this.producto.categoria === 'estanteria') {
      this.config = {
        alto: lim?.['alto']?.defecto ?? 2.0,
        ancho: lim?.['ancho']?.defecto ?? 1.2,
        profundidad: lim?.['profundidad']?.defecto ?? 0.6,
        tamanoBaldas: lim?.['tamanoBaldas']?.defecto ?? 0.02,
        numBaldas: (lim?.['numBaldas']?.defecto ?? 4) - 2,
        material: 'Abedul',
        margenSuperior: 0.3,
        margenInferior: 0.3,
        margenSimetrico: true
      } as ConfigEstanteria;
      this.actualizarLimiteBaldas();
    }
  }

  construirEsquemaConfig() {
    const lim = this.producto?.limites ?? {};

    if (this.producto.categoria === 'puerta') {
      this.esquemaConfig = [
        { clave: 'alto', etiqueta: 'Alto de la puerta', tipo: 'range', unidad: 'm', min: lim['alto']?.min ?? 1.5, max: lim['alto']?.max ?? 2.8, step: 0.01 },
        { clave: 'ancho', etiqueta: 'Ancho de la puerta', tipo: 'range', unidad: 'm', min: lim['ancho']?.min ?? 0.7, max: lim['ancho']?.max ?? 1.5, step: 0.01 },
        { clave: 'grosorDelMarco', etiqueta: 'Grosor del marco', tipo: 'range', unidad: 'm', min: lim['grosorDelMarco']?.min ?? 0.05, max: lim['grosorDelMarco']?.max ?? 0.2, step: 0.01 },
        {
          clave: 'material', etiqueta: 'Material o acabado', tipo: 'select',
          opciones: (this.producto.materiales ?? []).map(m => ({
            value: m.id, label: m.nombre, extra: m.sobreprecio > 0 ? `(+${m.sobreprecio}€)` : undefined
          }))
        },
        {
          clave: 'pomo', etiqueta: 'Tipo de apertura', tipo: 'select',
          opciones: [
            { value: 'Manillar', label: 'Manillar clásico' },
            { value: 'Pomo', label: 'Pomo redondo' }
          ]
        },
        { clave: 'cerradura', etiqueta: 'Añadir cerradura de seguridad con llave (+25€)', tipo: 'checkbox' }
      ];

    } else if (this.producto.categoria === 'ventana') {
      this.esquemaConfig = [
        { clave: 'alto', etiqueta: 'Alto de la ventana', tipo: 'range', unidad: 'm', min: lim['alto']?.min ?? 0.5, max: lim['alto']?.max ?? 3.0, step: 0.01 },
        { clave: 'ancho', etiqueta: 'Ancho de la ventana', tipo: 'range', unidad: 'm', min: lim['ancho']?.min ?? 0.5, max: lim['ancho']?.max ?? 3.0, step: 0.01 },
        { clave: 'grosorDelMarco', etiqueta: 'Grosor del marco exterior', tipo: 'range', unidad: 'm', min: lim['grosorDelMarco']?.min ?? 0.05, max: lim['grosorDelMarco']?.max ?? 0.3, step: 0.01 },
        { clave: 'perfilDelExterior', etiqueta: 'Grosor del marco exterior', tipo: 'range', unidad: 'm', min: lim['perfilDelExterior']?.min ?? 0.03, max: lim['perfilDelExterior']?.max ?? 0.15, step: 0.01 },
        {
          clave: 'material', etiqueta: 'Material o acabado', tipo: 'select',
          opciones: (this.producto.materiales ?? []).map(m => ({
            value: m.id, label: m.nombre, extra: m.sobreprecio > 0 ? `(+${m.sobreprecio}€)` : undefined
          }))
        }
      ];

    } else if (this.producto.categoria === 'estanteria') {
      this.esquemaConfig = [
        { clave: 'alto', etiqueta: 'Alto total del armario', tipo: 'range', unidad: 'm', min: lim['alto']?.min ?? 1.0, max: lim['alto']?.max ?? 3.0, step: 0.05 },
        { clave: 'ancho', etiqueta: 'Ancho total', tipo: 'range', unidad: 'm', min: lim['ancho']?.min ?? 0.5, max: lim['ancho']?.max ?? 2.5, step: 0.05 },
        { clave: 'profundidad', etiqueta: 'Profundidad de fondo', tipo: 'range', unidad: 'm', min: lim['profundidad']?.min ?? 0.3, max: lim['profundidad']?.max ?? 0.9, step: 0.02 },
        { clave: 'tamanoBaldas', etiqueta: 'Grosor / tamaño de las baldas', tipo: 'range', unidad: 'm', min: lim['tamanoBaldas']?.min ?? 0.015, max: lim['tamanoBaldas']?.max ?? 0.06, step: 0.005 },
        { clave: 'numBaldas', etiqueta: 'Número total de baldas interiores', tipo: 'range', min: 2, max: this.limiteMaxBaldasActual - 2, step: 1 },
        {
          clave: 'material', etiqueta: 'Material del armario', tipo: 'select',
          opciones: [
            { value: 'Abedul', label: 'Madera de Abedul Estándar' },
            { value: 'Cerezo', label: 'Madera de Cerezo Premium' },
            { value: 'Nogal', label: 'Madera de Nogal Macizo', extra: '(+45€)' }
          ]
        },
        { clave: 'margenSimetrico', etiqueta: 'Mantener el mismo margen en la zona superior e inferior', tipo: 'checkbox' },
        { clave: 'margenSuperior', etiqueta: 'Margen libre superior', tipo: 'range', unidad: 'm', min: lim['margenSuperior']?.min ?? 0.1, max: lim['margenSuperior']?.max ?? 0.6, step: 0.01 },
        {
          clave: 'margenInferior', etiqueta: 'Margen libre inferior', tipo: 'range', unidad: 'm',
          min: lim['margenInferior']?.min ?? 0.1, max: lim['margenInferior']?.max ?? 0.6, step: 0.01,
          soloSiClave: 'margenSimetrico', soloSiValorEsperado: false
        }
      ];

    } else {
      this.esquemaConfig = Object.keys(lim).map(clave => ({
        clave,
        etiqueta: clave.charAt(0).toUpperCase() + clave.slice(1),
        tipo: 'range' as const,
        unidad: 'm',
        min: lim[clave]?.min ?? 0,
        max: lim[clave]?.max ?? 10,
        step: 0.01
      }));
    }
  }
}