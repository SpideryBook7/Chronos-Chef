# Chronos Chef — candidata beta 0.3.0-beta.1

## Contenido cerrado para esta beta

Portada ilustrada con Jugar, Continuar, Entrenamiento, Ajustes y Créditos; selector de diez turnos; dos eras con entornos propios; dos recetas completas; cuatro especies de clientes; paciencia, reputación, propinas, combos, estrellas, monedas, gemas, mejoras, anomalías y clon de 15 segundos.

Entrenamiento guiado de once pasos, acción siguiente encolada durante el corte, recetario y confirmación antes de sustituir un guardado. Animaciones por cuadros de Ares, comida transportada independiente, clientes que llegan/salen, levitación, señales visuales, vapor, música y efectos. Movimiento reducido y volúmenes persistentes.

Las rutas bloquean las estaciones con margen para el chef. Los clientes están dentro de tres ventanas de recorte fijas alineadas al borde posterior de la barra, no en máscaras que se desplazan con el personaje. Los pedidos y los puntos de toque se recolocaron; se comprobó la entrega en cuerpo, cabeza y tarjeta. Paradox Alert aparece solo durante la anomalía activa.

Guardado local del perfil y de la partida: cada cinco segundos, pausa, portal y cierre. La recuperación conserva comida, trabajo/cocción, clientes, reloj y anomalía, y entra en pausa. Suspender la app pausa audio/juego y guarda; regresar requiere continuar manualmente. Guardados corruptos se descartan sin bloquear la portada.

## Pruebas

| Comprobación | Resultado | Evidencia |
|---|---|---|
| Tutorial y campaña de diez niveles sin puntos artificiales ni mejoras | PASS, tres estrellas en los diez turnos | Tests/beta-result.txt |
| Entrega por EventSystem en cuerpo, cabeza y tarjeta; ambas recetas | PASS | Tests/expansion-playtest-result.txt |
| Quemado, basura, clon, pausa, fin y reinicio | PASS | Tests/playtest-result.txt |
| 1.764 rutas y segmentos; acceso a estaciones con clon | PASS | Tests/navigation-result.txt |
| Tres máscaras fijas y clipping real de imágenes de clientes | PASS | Tests/counter-mask-result.txt |
| Alerta oculta/inicio, visible/paradoja y oculta/expiración | PASS | Tests/alert-lifecycle-result.txt |
| Portada: 12 puntos pulsables en 1600×900, 2160×900 y 1200×900 | PASS | Tests/ui-aspects-result.txt |
| Suspensión simulada: comida guardada, juego/audio pausados y retorno seguro | PASS | Tests/mobile-pause-result.txt |
| Compilación externa Windows | Succeeded, cero errores y advertencias | Tests/build-validation-result.json |
| Arranque del ejecutable externo en segundo plano | Proceso vivo, cero excepciones gestionadas detectadas en el log | Tests/player-startup-result.json |

La campaña automática consiguió 10/9/11/12/12/11/12/12/14/15 entregas, respectivamente. Demuestra que los objetivos y la puerta de gemas son alcanzables; no reemplaza una evaluación humana de dificultad o diversión.

El arranque externo comprueba carga e inicialización. La jugabilidad y el aspecto se comprobaron en el Editor; no se automatizó una partida dentro del ejecutable externo.

## Entregables

- Escena: Assets/ChronosChef/Scenes/ChronosKitchen.unity.
- Compilación interna de validación: Builds/BetaValidation/ChronosChef.exe, acompañada de sus DLLs y carpeta de datos. No es un APK.
- Portada: Screenshots/beta-start-menu.png.
- Cocina y barra corregida: Screenshots/beta-kitchen.png.
- Paradoja/raptor: Screenshots/beta-paradox.png.
- Segunda era: Screenshots/beta-steampunk.png.
- Pantallas ancha/tableta: Screenshots/beta-home-wide.png y beta-home-tablet.png.
- Procedencia y prompts del arte: Docs/Beta-art-prompts.md.
- Copia anterior: Backups/BeforeBeta.zip.

## Móvil y alcance

Configurados orientación horizontal en ambos sentidos, área segura, objetivo de 60 fps en móvil, compresión Android de los nuevos recursos, icono propio y versión 0.3.0-beta.1. Se conserva la distribución física de las estaciones entre eras; cambian el entorno y los parámetros de turno.

Pendiente de la siguiente fase, conforme a la indicación del usuario: generar APK y validar teléfonos físicos (toque, muescas, audio, suspensión real, consumo y rendimiento sostenido). No se afirma validación en móvil ni publicación en tiendas. La beta es offline y no añade anuncios, cuentas ni compras reales.

## Revisión visual

Se revisaron portada, juego, personaje/atlas, situación de paradoja y Steampunk. Se hizo un pase adicional para corregir el recorte de los clientes y recuperar la reputación visible en el HUD.

Palette discipline: 4/5 — cian/violeta/oro conservan la dirección de la referencia.
Style uniformity: 4/5 — portada, personajes y escenarios ilustrados consistentes; iconos geométricos deliberados.
Lighting depth: 3/5 — volumen y contornos pintados; el entorno no recibe iluminación dinámica.
Silhouette: 4/5 — cuatro especies y chef diferenciables a escala de juego.
UI intentionality: 4/5 — portada propia, controles separados de la ilustración, adaptación a tres proporciones.
Feedback visibility: 4/5 — rutas, trabajo, cocción, comida, entrega y alerta visibles y con sonido.

Es una evaluación del agente sobre capturas, no una medición de calidad por usuarios.

## Implementación y referencias

Se aplicaron unity-cli/unity-pipeline, ui-ugui, imagegen y los apartados playable-loop, game-shell, game-feel, game-audio y visual-scorecard de craft-game-floors. Se conservó la dirección artística y paleta existentes. No se aplicaron recetas de iluminación 3D al entorno 2D; se usa la alternativa de mezcla de audio por código de game-audio.

Documentación de Unity 6000.3 consultada en el desarrollo: PlayerPrefs y AudioClip.Create. El comportamiento se verificó contra el Editor y sus paquetes instalados. El arte se generó con image_gen integrado y quedó dentro del proyecto.

No se añadieron paquetes durante esta fase. Se añadieron scripts de niveles/tutorial/partida/portada, atlas y PNG propios, configuración del icono y de orientación/texturas, pruebas y documentación. La escena conserva referencias a los recursos. No se sustituyó el progreso normal durante las pruebas: se usaron claves aisladas, salvo ajustes de audio/movimiento que se restauraron al valor anterior.

