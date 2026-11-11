# Aura Evolution - Proyecto Unidad 4

Proyecto para la materia de Diseño de Juegos II (7mo Semestre).  
Repositorio: https://github.com/Osvaldo8005/Aura_Evolution.git

## Descripcion del juego

Aura Evolution es un videojuego en 3D pensado para dispositivos moviles que estoy desarrollando en Unity. A lo largo de esta Unidad 4 fui construyendo el juego paso a paso, empezando por el movimiento tridimensional, la camara y los menus, luego sumando el sistema de puntaje, record y dificultad, despues implementando la generacion procedural infinita de niveles junto con el bucle de juego (*Game Loop*), mejorando la iluminacion y personalizacion visual del personaje y del fondo, y finalmente cerrando con la navegacion completa entre menus y dos niveles consecutivos usando `SceneManager`.

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

## Actividad 4.4: Iluminacion y Mejoras Visuales

En esta cuarta entrega trabaje en el apartado estetico y de iluminacion del juego para que el escenario en 3D y el personaje principal tuvieran una mejor presentacion visual y cambiaran de forma dinamica durante la partida:

### Cambio dinamico del color de fondo del entorno
- Programe el script `GestorVisual.cs` y lo conecte directamente con el `GameManager.cs`. Configure la camara principal (`Main Camera`) en modo `SolidColor` para poder controlar el color de fondo por codigo.
- Defini un arreglo de colores en el Inspector (`coloresFondoPorNivel`) para que cada vez que el jugador sube de nivel de dificultad, el fondo del escenario cambie suavemente usando `Color.Lerp` (pasando por tonos azul acero, cian oceanico, verde esmeralda, ambar, purpura y carmesi). Cuando el jugador pierde y se reinicia el bucle de juego, el fondo regresa al color del Nivel 1.
- Como mejora visual adicional, active una niebla lineal (`RenderSettings.fog` en modo `Linear`) que inicia a 38 metros de distancia y toma el mismo color del fondo en tiempo real. Asi las plataformas lejanas se difuminan suavemente con el horizonte sin empañar ni restarle nitidez al personaje principal.

### Personalizacion del color y propiedades visuales del personaje principal
- Dentro de `GestorVisual.cs` y `PlayerController3D.cs` agregue las funciones para modificar en tiempo de ejecucion el material del caballero (`New Material.mat`) a traves de su `Renderer`.
- Conforme avanza el nivel de dificultad, el Aura del caballero evoluciona cambiando el tinte de su material (`_BaseColor`) y ajustando de forma moderada las propiedades visuales de su armadura (`_Metallic` en `0.15` y `_Smoothness` en `0.25`), desactivando la emision plana para que no se queme la textura original y siempre conserve sus detalles y colores reales.
- Ademas agregue una luz puntual suave (`Luz_Aura_Caballero`) con baja intensidad (`0.32`) adherida al personaje que cambia al mismo color de su Aura sin sobreexponerlo, y deje configurada la tecla `C` para poder alternar y probar manualmente los diferentes colores del personaje y del fondo en cualquier momento.

### Gestion de iluminacion direccional, ambiental y Global Volume
- Ajuste la `Directional Light` de la escena con una rotacion e inclinacion adecuadas (`45, -35, 0`), un tono calido neutro, intensidad balanceada (`1.05`) y sombras suaves (`Soft Shadows`) para darle buen volumen a las plataformas y al caballero.
- Configure los parametros ambientales de `RenderSettings` en modo `Trilight` con una intensidad controlada (`0.75`) y ajuste la exposicion, el contraste y el `Bloom` en el `Global Volume` de URP (`SampleSceneProfile.asset`) para que el caballero se vea nitido y con buen contraste tanto en fondos oscuros como claros.

---

## Actividad 4.5: Interfaz y Navegacion

Para esta quinta entrega complete el flujo de navegacion entre escenas y diseñe los paneles de interfaz para poder jugar a traves de dos niveles consecutivos sin errores en la consola:

### Flujo de dos niveles consecutivos y condicion de victoria
- Cree una segunda escena de juego llamada `Nivel2` (`Assets/Scenes/Nivel2.unity`) y la agregue al `EditorBuildSettings.asset` junto con `MenuPrincipal` y `SampleScene` (Nivel 1) para que `SceneManager` pueda cargar cualquiera de las tres escenas sin lanzar errores.
- En el `GameManager.cs` programe una condicion de victoria para el Nivel 1: cuando el jugador alcanza la meta de 500 puntos (o sobrevive 35 segundos en el recorrido), el juego detiene al personaje de forma segura, guarda el record en `PlayerPrefs` y muestra en pantalla el panel de **Nivel 1 Completado**.
- Para el `Nivel2` configure una variacion visual y de dificultad desde el inicio: empieza con una paleta de colores purpura astral, plataformas con mayor separacion, obstaculos mas rapidos y una meta superior de 800 puntos para completar el segundo nivel. Tambien agregue una estela luminosa (`TrailRenderer`) detras del caballero que cambia de color junto con su Aura mientras se desplaza.

### Transicion limpia entre escenas con SceneManager e interfaces de usuario
- Dentro del panel de **Nivel Completado** puse el boton **Siguiente Nivel**, el cual limpia los estados de entrada, asegura que el `Time.timeScale` este en `1` y ejecuta `SceneManager.LoadScene("Nivel2")` para pasar del Nivel 1 al Nivel 2 de manera limpia. Al completar el Nivel 2, el panel muestra el mensaje de victoria final y permite volver a jugar desde el Nivel 1 o regresar al menu.
- Mejore la interfaz del `MenuPrincipal` (`MenuController.cs`) para que muestre el mejor record guardado y tenga botones directos para iniciar en el **Nivel 1**, ir al **Nivel 2** o **Salir del juego**. Ademas, tanto en el panel de **Game Over** como en el de **Nivel Completado** y en la esquina superior derecha del HUD deje botones funcionales para regresar al `MenuPrincipal` en cualquier momento.
- Como apoyo para probar rapido la transicion sin tener que esperar toda la partida, deje configurada la tecla `N` para completar el nivel actual al instante y abrir el panel de cambio de nivel.

---

## Controles para probar el proyecto

**En computadora:**
- **Moverse:** Teclas `W`, `A`, `S`, `D` o flechas del teclado.
- **Saltar:** `Barra espaciadora`.
- **Completar nivel actual rapido (prueba de transicion a Nivel 2):** Tecla `N`.
- **Cambiar color de Aura y fondo manualmente (prueba rapida):** Tecla `C`.
- **Reiniciar rapido al perder:** Tecla `R` o boton `Reintentar` en pantalla.
- **Regresar al menu:** Clic en el boton `Menu` arriba a la derecha o desde los paneles de fin de nivel.

**En celular (o con el mouse en pantalla):**
- **Moverse:** Botones de direccion en la esquina inferior izquierda.
- **Saltar:** Boton `Salto` en la esquina inferior derecha.
- **Regresar al menu:** Boton `Menu` arriba a la derecha.

## Escenas del proyecto

- `MenuPrincipal`: Pantalla de inicio con el record guardado y botones para jugar el Nivel 1, jugar el Nivel 2 o salir de la aplicacion.
- `SampleScene`: Escena del **Nivel 1** con meta de 500 puntos (o 35 segundos) y transicion hacia el Nivel 2 al completarse.
- `Nivel2`: Escena del **Nivel 2** con atmosfera astral, mayor velocidad de obstaculos, mayor distancia entre plataformas y meta de 800 puntos.
