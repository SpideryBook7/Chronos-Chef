# Chronos Chef — primera beta 0.3.0-beta.1

Unity 6000.3.28f1 · 2D URP · horizontal · sin conexión.
Escena inicial: Assets/ChronosChef/Scenes/ChronosKitchen.unity.

## Empezar

Pulsa Play. La portada tiene JUGAR, CONTINUAR TURNO (cuando hay guardado), ENTRENAMIENTO, AJUSTES y CRÉDITOS. JUGAR abre el portal de niveles y mejoras. La primera entrada al nivel 1 inicia el entrenamiento; después se puede repetir con ENTRENAMIENTO.

El tutorial enseña ambas recetas en once pasos, señala la estación siguiente y detiene la presión del reloj, la paciencia y el quemado. No concede monedas, estrellas ni desbloqueos de campaña.

- Burger: DODO → CORTE → PLASMA → esperar LISTO → PLASMA para retirar → MONTAJE → cliente que pida burger.
- Sopa: RAÍCES dos veces → PLASMA → esperar LISTO → PLASMA para retirar el bol → cliente que pida sopa.
- Toca la estación o su icono inferior para caminar e interactuar al llegar.
- Toca cuerpo, cabeza o tarjeta del cliente para entregar.
- Durante el corte puedes indicar la estación siguiente: se visitará cuando termine.
- La comida transportada se muestra en las manos. BASURA descarta ese objeto.
- RECETAS pausa y abre el recetario. PAUSA permite continuar, reiniciar o volver al portal. Escape/volver cierra ventanas o pausa.

## Campaña y mecánicas

Diez turnos configurados individualmente, dos recetas y cuatro especies en tres puestos. Grog ruge y reduce la paciencia de vecinos, BIP-08 tiembla al impacientarse, Rex penaliza más la reputación al arañar la barra y Zorg levita. Los clientes entran y salen detrás de ventanas de recorte fijas: la barra oculta el cuerpo que queda por debajo de su borde.

Cocción y margen de quemado independientes, basura, combos, propinas, reputación, estrellas y derrota por tiempo/reputación. El fogón funciona mientras Ares hace otras tareas. Las rutas evitan las estaciones con margen para sus pies. El clon aparece durante 15 segundos al alcanzar 100% de anomalía y no ocupa puntos de servicio. PARADOX ALERT solo es visible durante esa anomalía; el medidor permanece visible.

Cinco turnos jurásicos y cinco Steampunk, con entornos ilustrados propios. Comparten distribución física para conservar rutas e interacciones. Completar un nivel abre el siguiente; tres gemas abren la segunda era. Cada nivel da una gema solo la primera vez que obtiene tres estrellas. Botas y plasma tienen tres mejoras compradas con créditos ganados en juego.

## Guardado e interrupciones

Créditos, mejores estrellas, mejoras, niveles, era y entrenamiento completado se guardan localmente. La partida activa se guarda cada cinco segundos, al pausar, al salir al portal y al cerrar/suspender la aplicación. Incluye comida, cocción, clientes, reputación, anomalía y reloj. RECUPERAR/CONTINUAR la restaura en pausa. Comenzar otro turno desde el portal pide confirmar si sustituye un guardado.

Los guardados inválidos se descartan sin bloquear el inicio. No hay cuentas, servidor, anuncios ni compras reales. Los PlayerPrefs son un guardado local, no un sistema contra trampas.

## Arte, sonido y móvil

Portada e icono propios, escenarios ilustrados, atlas de cinco personajes y ocho cuadros de Ares: reposo, carrera, trabajo y estrés. La comida al correr es una capa independiente. Llegadas, levitación, vapor, rutas, preparación, entrega y paradojas tienen señales visuales.

Música y efectos originales sintetizados; volúmenes separados y persistentes, música atenuada en pausa y audio suspendido al pasar al fondo. AJUSTES incluye movimiento reducido.

Preparado para ambas orientaciones horizontales, área segura, objetivo de 60 fps en móvil y texturas de la beta con compresión ASTC para Android. La vista 16:9 se adapta con bandas cuando la proporción del dispositivo es distinta. La revisión de interfaz incluye 1600×900, 2160×900 y 1200×900.

## Código

KitchenGame y KitchenGame.Beta: estado del turno, recetas, presión, tutorial y recuperación.
KitchenLevel: configuración de los diez turnos.
KitchenRules: geometría, puntos de interacción y rutas.
KitchenProgress / KitchenSession: economía, perfil y partida local.
KitchenView y sus parciales: representación, interfaz, entorno, beta y portada.
KitchenAudio: música, efectos y capa de volumen.
KitchenStageScale: escala del escenario horizontal.
Editor/: creadores originales y pruebas automáticas; no se incluyen en el jugador.

La escena referencia los recursos. Las pantallas y capas interactivas se construyen al ejecutar; edita sus parámetros en los scripts de vista. Las herramientas de configuración están en Tools/.

## Pruebas y validación

- ChronosBetaPlaytest.Start(): tutorial, encolado de acción, recuperación de comida/cocción, diez turnos completos, economía, ajustes y guardado corrupto.
- ChronosExpansionPlaytest.Start(): ambas recetas y entregas por EventSystem en cabeza, cuerpo y tarjeta.
- ChronosPlaytest.Start(): quemado, basura, clon, pausa, final y reinicio.
- Tools/VerifyNavigation.cs: 1.764 recorridos y segmentos sin atravesar estaciones.
- Tools/VerifyCounterMask.cs: máscaras de clientes fijas en el borde de la barra.
- Tools/AuditHomeTargets.cs: zonas de toque en varias proporciones.

Resultados en Tests/. Las pruebas de campaña usan perfiles aislados y restauran el progreso normal. Ejecutar pruebas de juego solo en Play Mode. No tomar capturas durante simulaciones de puntero: la herramienta puede cambiar temporalmente la superficie de render del Editor.

Compilación de validación de Windows: Builds/BetaValidation/ChronosChef.exe (requiere también su carpeta de datos y DLLs). Es una comprobación fuera del Editor, no un APK.

## Estado de la beta

Las mecánicas de esta primera beta están implementadas y verificadas en Editor y mediante compilación de Windows. El equilibrio se comprobó con un jugador automático; todavía requiere valoración humana. El contenido visual usa animación básica por cuadros y efectos procedurales.

La siguiente validación es en teléfonos físicos: rendimiento sostenido, audio, muescas/área segura, controles y restauración tras suspensión real de Android. No se ha generado APK ni probado en teléfono, de acuerdo con la indicación de terminar primero el juego. No se ha publicado en tiendas.

