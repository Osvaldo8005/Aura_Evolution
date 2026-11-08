# Aura Evolution - Proyecto Unidad 4

Proyecto para la materia de Diseño de Juegos II (7mo Semestre).  
Repositorio: https://github.com/Osvaldo8005/Aura_Evolution.git

## Descripcion del juego

Aura Evolution es un videojuego en 3D pensado para dispositivos moviles que estoy desarrollando en Unity. A lo largo de esta Unidad 4 fui construyendo el juego paso a paso, empezando por el movimiento tridimensional, la camara y los menus, luego sumando el sistema de puntaje, record y dificultad, y finalmente implementando la generacion procedural infinita de niveles junto con el bucle de juego (*Game Loop*).

---

## Actividad 4.1: Interfaces, Movimiento 3D y Camara

En esta primera parte me enfoque en armar la base jugable del proyecto y la navegacion entre escenas:

### Menu principal e interfaz tactil para movil
- Cree la escena `MenuPrincipal` con una interfaz adaptada a diferentes resoluciones de celular (`CanvasScaler`), incluyendo los botones para iniciar la partida o salir del juego (`MenuController.cs`).
- Dentro de la escena de juego (`SampleScene`), agregue controles tactiles en pantalla (`BotonTactil.cs`) con una cruceta de direccion a la izquierda, un boton de salto a la derecha y un boton arriba para regresar al menu en cualquier momento (`BotonMenuAccion.cs`).

### Desplazamiento en los 3 ejes y fisicas
- Programe el controlador del personaje (`PlayerController3D.cs`) usando `Rigidbody` y `BoxCollider` para que se mueva en los tres ejes: hacia los lados (eje X), hacia adelante y atras (eje Z) y con salto fisico aplicando fuerza vertical (eje Y).
- Configure la deteccion de suelo mediante colisiones para que solo pueda saltar cuando esta apoyado sobre una superficie.

### Seguimiento de camara estable
- Implemente el script `CameraFollow3D.cs` para que la camara siga de manera suave al personaje en los ejes X y Z manteniendo una distancia constante detras del jugador y una vista estable sin giros bruscos.

---

## Actividad 4.2: Puntaje, Curva de Dificultad y PlayerPrefs

En la segunda entrega segui trabajando sobre la base anterior para agregar la progresion del jugador y el guardado de datos:

### Contador visual en la interfaz
- Agregue en la parte superior de la pantalla los textos de puntaje y distancia recorrida en metros usando TextMeshPro.
- El contador sube de forma constante y uniforme mientras el personaje avanza por el escenario. Ademas, puse monedas coleccionables (`Coleccionable.cs`) que giran en el mapa y que al tocarlas suman puntos extra al instante.

### Curva de dificultad progresiva
- Cree en el `GameManager.cs` un sistema que aumenta el nivel de dificultad conforme avanza el tiempo y la distancia recorrida.
- Cada vez que sube el nivel de dificultad, aumenta un porcentaje la velocidad del jugador y los obstaculos (`GeneradorObstaculos.cs`) salen mas rapido y con mayor frecuencia. En la esquina superior derecha se muestra en todo momento el nivel de dificultad actual y la velocidad.

### Guardado persistente del record con PlayerPrefs
- Para que el puntaje maximo no se pierda al cerrar el juego o reiniciar, utilice `PlayerPrefs.SetInt` y `PlayerPrefs.Save` cada vez que se supera la marca anterior.
- Al iniciar la escena, el juego recupera el record guardado con `PlayerPrefs.GetInt` y lo muestra en pantalla.

---

## Actividad 4.3: Niveles de Juego Procedurales y Game Loop

Para esta entrega desarrolle la mecanica de generacion dinamica de niveles en tiempo de ejecucion, apoyandome en la logica vista en los videos de niveles y *DeathPart*:

### Generacion procedural infinita de niveles
- Programe el script `GeneradorNiveles.cs` junto con `ModuloNivel.cs` para que el escenario ya no sea un solo piso fijo, sino que se vaya generando por modulos o plataformas de manera infinita mientras el jugador avanza hacia adelante.
- Conforme el personaje avanza, el codigo instancia nuevas secciones de nivel por delante y destruye automaticamente las plataformas que ya quedaron atras para no saturar la memoria.
- Cada seccion se compone de varias porciones, monedas coleccionables, obstaculos que patrullan de lado a lado y zonas rojas de peligro (*DeathParts* controladas por `ZonaMuerte.cs`). Tambien me asegure de que las primeras plataformas al iniciar sean seguras para que el jugador no pierda apenas empieza.

### Configuracion de parametros desde el Inspector de Unity
- Diseñe los scripts con encabezados (`[Header]`), descripciones (`[Tooltip]`) y rangos (`[Range]`) para poder modificar facilmente el comportamiento del nivel directo desde el Inspector sin tener que tocar el codigo:
  - Cantidad de plataformas activas al mismo tiempo y distancia a la que aparecen o se borran.
  - Tamaño de cada plataforma, **distancia (hueco) entre plataformas**, variacion lateral en X y variacion de altura en Y.
  - Cantidad de porciones que forman cada seccion del nivel y cuantas zonas de peligro (*DeathParts*) se activan.
  - Velocidad y probabilidad de aparicion de los obstaculos y monedas en cada plataforma.

### Conexion con la curva de dificultad y Bucle de Juego (Game Loop)
- Conecte el `GeneradorNiveles` con la curva de dificultad progresiva que ya tenia en el `GameManager`: conforme sube el nivel de dificultad, las nuevas plataformas que se van generando tienen mayor separacion entre si, activan mas porciones rojas de peligro y sus obstaculos se mueven mas rapido.
- Implemente el **Game Loop** completo para cuando el jugador pierde (ya sea por tocar una zona roja *DeathPart*, chocar con un obstaculo o caerse al vacio entre las plataformas):
  - Al perder, se guarda el record en `PlayerPrefs`, se muestra el aviso de fin de partida en pantalla y el bucle reinicia el flujo del juego automaticamente (o al presionar el boton de **Reintentar** / tecla `R`), regresando el puntaje actual a 0, la dificultad al Nivel 1, al jugador a la plataforma inicial y generando un nivel procedural nuevo desde cero.

---

## Controles para probar el proyecto

**En computadora:**
- **Moverse:** Teclas `W`, `A`, `S`, `D` o flechas del teclado.
- **Saltar:** `Barra espaciadora`.
- **Reiniciar rapido al perder:** Tecla `R` o boton `Reintentar` en pantalla.
- **Regresar al menu:** Clic en el boton `Menu` arriba a la derecha.

**En celular (o con el mouse en pantalla):**
- **Moverse:** Botones de direccion en la esquina inferior izquierda.
- **Saltar:** Boton `Salto` en la esquina inferior derecha.
- **Regresar al menu:** Boton `Menu` arriba a la derecha.

## Escenas del proyecto

- `MenuPrincipal`: Pantalla de inicio con los botones para entrar al juego o salir de la aplicacion.
- `SampleScene`: Escena principal donde se ejecuta el generador procedural de niveles, el jugador, los obstaculos, los coleccionables y toda la interfaz del juego.
