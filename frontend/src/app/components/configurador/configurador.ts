import { Component, ElementRef, OnInit, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { Mueble, ConfigPuerta, ConfigEstanteria, ConfiguracionMueble } from '../../interfaces/mueble';

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
export class Configurador implements OnInit {

  @ViewChild('unityCanvas') unityCanvasRef!: ElementRef<HTMLCanvasElement>;
  unityCargado: boolean = false;
  progresoCarga: number = 0;
  private unityYaSolicitado: boolean = false;

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
  camara = { x: 45, y: 0, z: 0 };

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
            { id: 'PvcGris', nombre: 'Plástico PVC Gris', sobreprecio: 0 }
          ],
          limites: {
            alto: { min: 1.8, max: 2.5, defecto: 2.1 },
            ancho: { min: 0.6, max: 1.3, defecto: 0.9 },
            grosor: { min: 0.05, max: 0.2, defecto: 0.1 }
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
            { clave: 'Ancho', valor: '1.2m' }
          ],
          limites: {
            alto: { min: 0.6, max: 1.8, defecto: 1.2 },
            ancho: { min: 0.6, max: 2.0, defecto: 1.2 }
          }
        };
        break;

      case 'E001': // CASO 1: Armario grande (Mín 1.0m / Máx 2.0m / Defecto 1.2m)
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
        grosor: lim?.['grosor']?.defecto ?? 0.1,
        material: 'Abedul',
        pomo: 'Manillar',
        cerradura: false
      } as ConfigPuerta;

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
        { clave: 'grosor', etiqueta: 'Grosor del marco', tipo: 'range', unidad: 'm', min: lim['grosor']?.min ?? 0.05, max: lim['grosor']?.max ?? 0.2, step: 0.01 },
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
            { value: 'Nogal', label: 'Madera de Nogal Macizo', extra: '(+45€)' },
            { value: 'PvcGris', label: 'Plástico PVC Gris Económico', extra: '(-20€)' }
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

  // ---- CONTROL DE VISTAS ----
  activarModoMedida() {
    this.modoAmedida = true;
    setTimeout(() => this.cargarUnity(), 0);
  }

  terminarProducto() {
    this.modoAmedida = false;
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

  // Lógica reactiva ante cambios en los inputs
  alCambiarParametro(parametro: string) {
    // 1. Validar y encajonar el valor dentro de sus límites dinámicos si existen
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

    // 2. Lógica de simetría de márgenes para estanterías
    if (this.producto.categoria === 'estanteria' && this.config.margenSimetrico) {
      if (parametro === 'margenSuperior') this.config.margenInferior = this.config.margenSuperior;
      if (parametro === 'margenInferior') this.config.margenSuperior = this.config.margenInferior;
    }

    if (this.producto.categoria === 'estanteria' &&
      ['alto', 'profundidad', 'tamanoBaldas', 'margenSuperior', 'margenInferior'].includes(parametro)) {
      this.actualizarLimiteBaldas();
    }
    // >>> CAMBIO FIN

    this.calcularPrecio();

    // "MÉTODO JSON vs SETTERS INDIVIDUALES". Por defecto seguimos
    // usando el envío atómico de un solo parámetro:
    // this.enviarAUnity(`Set${parametro.charAt(0).toUpperCase() + parametro.slice(1)}`, this.config[parametro]);

    // Para usar SIEMPRE el método JSON en su lugar, comenta la línea de
    // arriba y descomenta esta otra:
    this.enviarAUnity('AplicarConfiguracion', JSON.stringify(this.config));
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

    // Reajusta el propio esquema (para que el HTML vea el nuevo max)
    const campoBaldas = this.esquemaConfig.find(c => c.clave === 'numBaldas');
    if (campoBaldas) campoBaldas.max = maxUsuario;

    if (this.config.numBaldas > maxUsuario) {
      this.config.numBaldas = maxUsuario;
    }
  }
  // >>> CAMBIO FIN

  // ---- MOTOR DE CÁLCULO POLIMÓRFICO ----
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

  // ---- CÁMARA Y CONEXIÓN MOTOR 3D ----
  resetearCamara() {
    this.camara = { x: 45, y: 0, z: 0 };
    this.alCambiarCamara();
  }

  alCambiarCamara() {
    this.enviarAUnity('ActualizarCamara', `${this.camara.x},${this.camara.y},${this.camara.z}`);
  }

  // MÉTODO JSON vs SETTERS INDIVIDUALES
  // Tienes dos formas de mandar la configuración completa a Unity, y AMBAS
  // ya están implementadas y funcionando en el código:
  //
  //  A) SETTERS INDIVIDUALES (Object.keys().forEach): manda un SendMessage
  //     por cada propiedad de `config` (SetAlto, SetAncho, SetMaterial...).
  //     Es lo que se usa ahora mismo aquí abajo.
  //
  //  B) JSON ÚNICO (AplicarConfiguracion): manda TODO `config` serializado
  //     en un solo SendMessage, y Unity lo parsea con JsonUtility.FromJson.
  //     Es más simple de mantener (un solo mensaje, no hay que acordarse de
  //     los nombres Set-de-cada-campo) pero para catual manual solo obtienes
  //     ventaja real cuando cambias VARIOS campos a la vez (como al resetear).
  //
  // Para usar SIEMPRE la opción B en vez de la A, sustituye el cuerpo de
  // este método por la línea comentada de abajo:
  private sincronizarTodoConUnity() {
    // OPCIÓN A (actual): un mensaje por cada campo
    // Object.keys(this.config).forEach(key => {
    //   const valor = typeof this.config[key] === 'boolean' ? (this.config[key] ? 1 : 0) : this.config[key];
    //   this.enviarAUnity(`Set${key.charAt(0).toUpperCase() + key.slice(1)}`, valor);
    // });

    // OPCIÓN B: descomenta esta línea y comenta el forEach de arriba
    this.enviarAUnity('AplicarConfiguracion', JSON.stringify(this.obtenerConfigParaUnity()));
  }

  private obtenerConfigParaUnity(): any {
    if (this.producto.categoria === 'estanteria') {
      return { ...this.config, numBaldas: this.config.numBaldas + 2 };
    }
    return this.config;
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

  private readonly metodosDeCamara = new Set(['ActualizarCamara', 'ActivarModoEdicion']);

  private enviarAUnity(metodo: string, valor: any) {
    try {
      if ((window as any).unityInstance) {
        const destino = this.metodosDeCamara.has(metodo)
          ? 'CamaraPrincipal'
          : this.obtenerNombreGameObjectUnity();
        (window as any).unityInstance.SendMessage(destino, metodo, valor);
      } else {
        console.info(`%c[Unity Mock]%c ${this.producto.categoria.toUpperCase()} -> ${metodo}(${valor})`, 'color: #2563eb; font-weight: bold;', 'color: inherit;');
      }
    } catch (e) {
      console.error('Error de comunicación WebGL', e);
    }
  }

  cargarUnity() {
    if (this.unityYaSolicitado) return;
    this.unityYaSolicitado = true;

    const buildUrl = 'unity/Build';

    const config = {
      dataUrl: `${buildUrl}/unity.data`,
      frameworkUrl: `${buildUrl}/unity.framework.js`,
      codeUrl: `${buildUrl}/unity.wasm`,
      streamingAssetsUrl: 'unity/StreamingAssets',
      companyName: 'TuEmpresa',
      productName: 'ConfiguradorMuebles',
      productVersion: '1.0',
    };

    const script = document.createElement('script');
    script.src = `${buildUrl}/unity.loader.js`;

    script.onload = () => {
      (window as any).createUnityInstance(
        this.unityCanvasRef.nativeElement,
        config,
        (progress: number) => {
          this.progresoCarga = Math.round(progress * 100);
        }
      ).then((instance: any) => {
        (window as any).unityInstance = instance;
        this.unityCargado = true;

        instance.SendMessage('GestorMuebles', 'MostrarCategoria', this.producto.categoria);

        this.enviarAUnity('ActivarModoEdicion', 1);
        this.sincronizarTodoConUnity();
        this.alCambiarCamara();
      }).catch((err: any) => {
        console.error('Error al iniciar la instancia de Unity', err);
      });
    };

    script.onerror = () => {
      console.error(`No se pudo cargar el loader de Unity en: ${script.src}`);
    };

    document.body.appendChild(script);
  }
}