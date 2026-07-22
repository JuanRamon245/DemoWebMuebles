export interface Rango {
    min: number;
    max: number;
    defecto: number;
}

export interface OpcionMaterial {
    id: string;
    nombre: string;
    sobreprecio: number;
}

export interface Mueble {
    id: string;
    nombre: string;
    categoria: 'puerta' | 'ventana' | 'estanteria' | 'mesa';
    precioBase: number;
    imagenes: string[];
    descripcion: string;
    atributosPorDefecto: Array<{ clave: string; valor: string }>;
    limites?: { [propiedad: string]: Rango };
    materiales?: OpcionMaterial[];
}

export interface ConfigPuerta {
    alto: number;
    ancho: number;
    grosorDelMarco: number;
    material: string;
    pomo: string;
    cerradura: boolean;
}

export interface ConfigVentana {
    alto: number;
    ancho: number;
    grosorDelMarco: number;
    perfilDelExterior: number;
    material: string;
}

export interface ConfigEstanteria {
    alto: number;
    ancho: number;
    profundidad: number;
    tamanoBaldas: number;
    numBaldas: number;
    material: string;
    margenSuperior: number;
    margenInferior: number;
    margenSimetrico: boolean;
}

export type ConfiguracionMueble = ConfigPuerta | ConfigEstanteria;