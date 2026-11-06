# Aura Evolution - Proyecto Unidad 4

Proyecto para la materia de Diseño de Juegos II (7mo Semestre).
Repositorio: https://github.com/Osvaldo8005/Aura_Evolution.git

## Descripcion del juego

Aura Evolution es un juego en 3D pensado para celulares que estoy desarrollando en Unity. En esta entrega de la Actividad 4.2 continue trabajando sobre la base de la actividad anterior y agregue mecanicas de avance, dificultad progresiva y guardado de datos.

## Que implemente en esta Actividad 4.2

Para esta entrega me enfoque en cumplir con los puntos que nos pidio el profesor:

### Contador visual en la interfaz

Agregue un contador de puntaje y de distancia recorrida en la parte superior de la pantalla utilizando la interfaz de usuario de Unity.

Este contador sube de forma constante y uniforme mientras mi personaje se va moviendo por el escenario. Si el personaje camina o salta, la distancia en metros y los puntos aumentan al mismo ritmo. Tambien coloque objetos coleccionables en el mapa (como monedas doradas) que al tocarlos le suman puntos extra al jugador al instante.

### Curva de dificultad progresiva

Cree un sistema dentro del GameManager que hace que el juego se vuelva mas desafiante conforme pasa el tiempo.

Cada 12 segundos el juego sube de nivel de dificultad. Cuando esto pasa, la velocidad del personaje aumenta un poco y los obstaculos del escenario comienzan a salir mas seguido y se mueven mas rapido. Esto hace que entre mas tiempo logres avanzar, mas rapido tengas que esquivar. En pantalla deje un texto que indica en que nivel de dificultad vas y a que velocidad te mueves.

### Guardado persistente del record con PlayerPrefs

Para que el puntaje maximo no se borre al cerrar el juego, utilice PlayerPrefs.

Cada vez que el puntaje actual supera el puntaje mas alto guardado, el juego lo guarda de inmediato en la memoria del dispositivo con PlayerPrefs.SetInt y PlayerPrefs.Save. Cuando vuelves a abrir el juego o reinicias la partida, el juego carga ese dato con PlayerPrefs.GetInt y lo muestra en el texto de Record. Tambien me asegure de guardar el progreso al chocar, al salir de la aplicacion o al volver al menu principal.

## Controles para probar el proyecto

En computadora:
- Moverse: Teclas W, A, S, D o flechas del teclado.
- Saltar: Barra espaciadora.
- Regresar al menu: Clic en el boton Menu de arriba a la derecha.

En celular (o con el mouse en pantalla):
- Moverse: Botones de direccion en la esquina inferior izquierda.
- Saltar: Boton Salto en la esquina inferior derecha.
- Regresar al menu: Boton Menu arriba a la derecha.

## Escenas del proyecto

- MenuPrincipal: Pantalla de inicio con los botones para jugar o salir.
- SampleScene: Escena principal donde esta el personaje, los obstaculos, los coleccionables y toda la interfaz del juego.
