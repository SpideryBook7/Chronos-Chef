# Revisión de desarrollo 0.2

## Fallo de entrega

La zona pulsable anterior del cliente abarcaba y=595..705, mientras que parte de su cabeza y su tarjeta quedaban fuera. Se amplió a y=619..841, incluyendo personaje y tarjeta sin invadir corte/fogón. La prueba nueva usa raycasts reales del EventSystem y PointerClick en cuerpo, cabeza y tarjeta, prepara las dos recetas recorriendo estaciones y verifica créditos y manos vacías tras entregar. La prueba anterior solo comprobaba el centro del botón.

## Ampliación

Sopa de dos raíces, recetas por cliente, combos, reputación, objetivos, cuatro especies con reacciones, diez turnos, progresión local, mejoras de movimiento/cocción, gemas sin premio duplicado y desbloqueo de era. Menús de portal/pausa/recetas/sonido/resultados. Sonidos sintetizados, música de fondo, volumen persistente y señales visuales de ruta, trabajo, cocción, margen de quemado, entrega y paradojas.

Personajes ilustrados y cocina ilustrada siguiendo la referencia: piedra morada, madera y circuitos, horno, almacenes, corte, plasma, montaje, mostrador, barra inferior y reloj de paradoja. El fondo sirve como entorno; personajes, pedidos, progreso, controles y comida son elementos dinámicos separados.

## Verificación y límites

Resultados funcionales en Tests/playtest-result.txt y Tests/expansion-playtest-result.txt. Cada prueba conserva el progreso normal y utiliza claves temporales aisladas. Las capturas se ejecutan separadas de las simulaciones de puntero, porque la captura del Game View puede cambiar momentáneamente su superficie de render. No equivale a una prueba en teléfono físico.

No se creó un APK. Se detectó soporte Android y se cambió temporalmente el destino para comprobar la configuración; se volvió a StandaloneWindows64 tras la indicación del usuario. Permanecen configurados nombre del producto, identificador com.chronoschef.game, versión 0.2.0 y ajustes Android ARM64/IL2CPP para más adelante. No se instalaron paquetes adicionales durante esta ampliación. Se importaron dos PNG generados y se modificaron scripts, escena y documentación. Copia previa en Backups/BeforeExpansion.zip.

Steampunk sigue compartiendo cocina y recetas; animación por movimiento y efectos procedurales, no animaciones completas por spritesheet. Audio sintetizado, no banda sonora final. Sin publicación, servidor o compras reales.

## Revisión visual (escala 1–5, evaluación del agente)

Captura: Screenshots/chronos-reference-kitchen.png. Se aplicó un segundo pase: ocultación de piernas detrás del mostrador, ingredientes en burbujas y reducción del HUD a la composición de referencia.

Palette discipline: 4/5 — cian, violeta y oro mantienen la identidad de la referencia.
Style uniformity: 3/5 — cocina y personajes ilustrados; iconos todavía geométricos.
Lighting depth: 3/5 — contornos y volumen pintado consistentes; no luces dinámicas sobre el fondo.
Silhouette: 4/5 — chef y cuatro clientes diferenciados por silueta y accesorios.
UI intentionality: 4/5 — estaciones alineadas, pedidos, barra inferior y alerta propias del juego.
Feedback visibility: 3/5 — ruta, vapor, progreso, comida y entrega visibles; animaciones completas pendientes.

Antes del pase visual: uniformidad 2/5, silueta 2/5 y feedback 2/5. El resto ya tenía paleta e interfaz deliberadas. No se considera una versión terminada.

## Habilidades y referencias consultadas

unity-cli, unity-pipeline, ui-ugui, sprite-editor, imagegen y craft-game-floors. De craft-game-floors: art-direction.md, placeholder-assets.md, playable-loop.md, game-feel.md, game-audio.md, game-shell.md y visual-scorecard.md. No se aplicaron recetas de iluminación 3D al proyecto 2D. La mezcla usa la alternativa por código de game-audio, no un AudioMixer.

Documentación de Unity 6000.3 consultada: [PlayerPrefs](https://docs.unity.com/en-us/engine/6000.3/script-reference/unityengine/playerprefs) y [AudioClip.Create](https://docs.unity.com/en-us/engine/6000.3/script-reference/unityengine/audioclip/create). El enlace de RaycastAll no estuvo disponible; se verificó el comportamiento ejecutando el EventSystem instalado en el Editor.
