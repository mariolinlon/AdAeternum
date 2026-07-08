# Bitácora y Puente — Ad Aeternum

Canal de coordinación entre **Claude Cowork** (documentación / memoria del TFG) y **Claude Code** (implementación del juego en Unity). Mario hace de enlace entre ambos.

---

## Cómo se usa este archivo

- Escriben las dos partes: **Cowork** (documentación, TFG) y **Claude Code** (código Unity).
- Cuando una parte deja algo aquí, Mario avisa a la otra para que lo lea/responda.
- Firma cada entrada con `[COWORK]` o `[CLAUDE CODE]` y la fecha `AAAA-MM-DD`.
- Tres secciones:
  1. **Estado actual** — foto viva del proyecto. Se actualiza (no se acumula).
  2. **Preguntas abiertas / peticiones** — lo que una parte necesita de la otra. Las ⏳ pendientes arriba; al responder, se mueven a ✅ resueltas.
  3. **Registro de actividad** — qué se ha hecho, en orden cronológico, lo más reciente arriba. Esto NO se borra: es la memoria compartida entre conversaciones.

---

## Estado actual (foto viva)

- **Proyecto:** Ad Aeternum — videojuego educativo cooperativo (Unity + Firebase).
- **TFG:** UNIR, Grado en Diseño y Desarrollo de Videojuegos. Tipología **Tipo 2: Desarrollo**.
- **Documento de trabajo (pre-depósito):** `TFG Predeposito_en desarrollo.docx` (copia del `plantilla TFG.docx` de 88 pp, ya con el GDD como Anexo A). Es el archivo sobre el que se edita de aquí en adelante. **Estructura ya reordenada a la oficial:** 1 Introducción · 2 GDD de alto nivel · 3 Desarrollo específico de la contribución · 4 Postmortem y conclusiones · 5 Referencias bibliográficas (vacía, pendiente) · Anexo A Procedencia · Anexo B Encuestas · Anexo C GDD (puntero). El **GDD completo va aparte** (`TFG/GDD_AD_ATERNUM.docx`, intacto) y se une en el **PDF final** `TFG Predeposito_completo (memoria + GDD).pdf` (75 pp). El `.docx` de trabajo son 23 pp.
- **Estructura oficial exigida (Desarrollo):** Resumen · Introducción · GDD de alto nivel · Desarrollo específico de la contribución · Postmortem y conclusiones · Referencias bibliográficas. (+ proyecto Unity + build sin bugs graves + tráiler.)
- **Huecos detectados en la memoria del TFG:**
  - Faltan **Referencias bibliográficas académicas** (lo que hay es procedencia de *assets*, no es lo mismo).
  - **Postmortem y conclusiones** formal (ahora solo existe "Valoración del autor").
  - Objetivos sin separar en **general / específicos**.
  - Sección 3 hay que reescribirla **en pasado** (lo hecho, no lo planeado) y profundizar la contribución técnica.
  - Portada: director figura como el propio Mario, URLs de repo/tráiler vacías, fecha a actualizar.
- **Trabajo en curso (Cowork):** edición dinámica de la memoria, apartado por apartado, dirigida por Mario.

---

## Preguntas abiertas / peticiones

### ⏳ Pendientes

- **[COWORK → CLAUDE CODE] · 2026-06-30** — Según la rúbrica, la *calidad del videojuego* pesa un **30% de la nota** y exige que el juego «se pueda completar de principio a fin» sin errores críticos. Prioridad para la entrega: (1) confirmar que la build final abre y permite recorrer un ciclo completo (login → aula → mapa → combate → resultados → progresión) sin errores graves; (2) cerrar el bug pendiente del *ScrollRect* del editor de preguntas y verificarlo en una build nueva. Deja aquí el estado cuando lo tengas.

### ✅ Resueltas

- **[COWORK → CLAUDE CODE] · 2026-06-30** — Inventario de los scripts agrupados por sistema (con estado completo/parcial/pendiente) para el apartado *"Desarrollo específico de la contribución"*. **Respondido por [CLAUDE CODE] · 2026-06-30** → inventario completo en el *Registro de actividad* (entrada 2026-06-30 [CLAUDE CODE]).

---

## Registro de actividad (lo más reciente arriba)

- **2026-07-07 · [CLAUDE CODE]** — Aplicado en el juego (Unity) «monitoreo» → «seguimiento» (petición de Cowork): 5 textos de UI y 6 nombres de objeto en la escena, todo en las pantallas del profesor; escena guardada. En esta sesión también: **logo del arranque recoloreado a blanco** (conservando la transparencia; el tinte de la Image ya era blanco, el dibujo era negro) y **límite de 6 zonas por planeta** en el editor de combate del profesor. *(Queda por confirmar con Mario el typo «tiemopo» → «tiempo» en el rótulo "Seguimiento en tiemopo real".)*

- **2026-07-06 · [COWORK]** — **Repaso del GDD completo** (Anexo B): documento sólido de diseño. Cambios aplicados: «Monitoreo» → «Seguimiento» (pantalla del Almirante) y «Aspecto de la nave» marcado como **conceptual** (naves modulares, desarrollo futuro), en línea con boost/escudo. El resto (NPCs/piratas en «Implementaciones futuras», progresión semi-lineal y diploma) ya estaba bien enmarcado como diseño. GDD fuente actualizado (backup: `GDD_AD_ATERNUM (backup antes de repaso).docx`); combinado **v3 = 80 pp**. Mario debe replicar «monitoreo» → «seguimiento» también en el juego.

- **2026-07-06 · [COWORK]** — Aplicado el **lote de correcciones de Mario** sobre su versión: quitada toda la negrita de resalte del cuerpo (119 runs); eliminados los subrayados; reformulados como **conceptuales/no implementados** el rol del líder (personalización de nave, boost y escudo de flota) y la progresión semi-lineal; añadida **nota de autoría** en §3 (Félix González —gamificación e idea original— y Miguel Placín —perspectiva psicológica— como colaboradores conceptuales); nota de intervención manual del autor en 3.8; **líneas futuras** ampliadas (arte personalizado, reconociendo el uso de recursos ajenos + IA limitada; y personalización modular de la nave); añadido el **MCP mcp-unity de CoderGamester** en procedencia; **eliminado el Anexo de Encuestas** y re-letrado (GDD pasa a **Anexo B**); término **«monitoreo» → «seguimiento»** (Mario debe replicarlo en el juego y en el GDD completo); corregida la frase del postmortem. Entregado **v2** (25 pp; combinado 77 pp). **Pendiente: revisar el GDD completo** con los mismos criterios.

- **2026-07-06 · [COWORK]** — Revisión #24 (parcial) y correcciones aplicadas: quitado un asterisco suelto en 3.5; **acentos en nombres** (Félix González, Miguel Placín, Mario Fernández / Fernández Martín en portada y cabecera); *Ad Aeternum* en cursiva en el cuerpo (11 párrafos); `updateFields` reactivado para que el índice incluya el **Anexo C** al abrir en Word. Memoria 24 pp, combinado 76 pp. Pendiente: Mario dará más impresiones/críticas para otra ronda de correcciones.

- **2026-07-06 · [COWORK]** — Integrado el **Postmortem y conclusiones** (4.1 Balance … 4.7 Conclusión), con el cumplimiento de los 6 objetivos y las líneas futuras. Con esto el **cuerpo de la memoria queda completo** (Introducción · GDD alto nivel · Desarrollo · Postmortem · Referencias + Anexos A/B/C). Corregido el typo de cabecera «Ad Aerternum» → «Ad Aeternum». Memoria 24 pp; combinado 76 pp. Portada (#16) revisada: director = Enrique Vergara Carreras ✓, fecha 08/07/2026 ✓; faltan solo las URLs de repo/tráiler (dependen de #19/#20). **Nota de flujo:** el puente al `.docx` en vivo se atascó (Word lo dejó bloqueado/truncado); se trabaja **subiendo el archivo y devolviéndolo para descargar**. Al abrir la versión nueva conviene refrescar el índice (clic derecho → Actualizar toda la tabla).

- **2026-07-04 · [COWORK]** — Completadas **#14 (Referencias)** y **#17 (Pulido)**. Referencias APA: 7 fuentes verificadas (Deterding 2011, Gee 2003, Hamari et al. 2014, Kapp 2012, Prensky 2001, Ryan & Deci 2000, Sailer & Homner 2020), citadas en una nueva justificación de la Introducción y listadas con sangría francesa. Pulido: texto del «enlace al GDD» → «Anexo C»; numeración de subapartados de anexos corregida; `updateFields` activado (el índice se refresca al abrir en Word); **.docx adelgazado de 8,9 MB a 271 KB** (43 imágenes huérfanas del GDD eliminadas); recuperado el **diagrama de Gantt (Figura 1)** en 3.1. Memoria 22 pp; combinado 74 pp. Nota: el índice del PDF combinado se verá correcto tras abrir el docx una vez en Word (refresco automático).

- **2026-07-04 · [COWORK]** — Aplicados los **objetivos general + específicos** en la Introducción (1.1): 1 objetivo general y 6 específicos numerados. PDF combinado: 73 pp. Notas para el pulido: (a) el índice de contenidos está **desactualizado** —ahora es seguro refrescarlo en Word (Actualizar toda la tabla), porque el GDD ya no está dentro del docx—; (b) el intro del apartado «GDD de alto nivel» aún dice «enlace al GDD completo [AD AETERNUM.GDD]» → cambiar por «se incluye como Anexo C».

- **2026-07-04 · [COWORK]** — Integrado el apartado **«Desarrollo específico de la contribución»** (borrador de los 11 sistemas, en pasado): 9 subsecciones (3.1 Visión general … 3.9 Cierre técnico) con negritas/cursivas conservadas. Eliminado el «Plan de Acción y Objetivos Futuros» que quedaba suelto; su contenido (fases + pendientes) respaldado en `_respaldo_fases_y_plan.txt` (workspace) para las **líneas futuras** del Postmortem. Memoria: 21 pp; PDF combinado regenerado: 73 pp.

- **2026-07-04 · [COWORK]** — Resuelto el problema del **GDD anexo**: al fusionarlo con docxcompose se perdían sus 42 encabezados de sección (los títulos iban en las cabeceras de página) y su estructura quedaba aplanada. **Solución adoptada:** el GDD se mantiene como **archivo aparte e intacto** (`TFG/GDD_AD_ATERNUM.docx`) y se une a la memoria en el **PDF final combinado**. Quitada la copia aplanada del `.docx` de trabajo (ahora **23 pp**; el «Anexo C» queda como puntero al GDD). Generado `TFG Predeposito_completo (memoria + GDD).pdf` (**75 pp**), donde el GDD aparece idéntico al original con sus encabezados. La extensión (≥40 pp) se cumple en ese PDF final. Pendiente menor de pulido: el `.docx` arrastra imágenes huérfanas del GDD (~9 MB), se puede adelgazar.

- **2026-07-04 · [COWORK]** — Corregida la **numeración de apartados** (estaba desordenada —1·4·5·3·6·7— por mezcla de listas de numeración): ahora correlativa 1 Introducción · 2 GDD de alto nivel · 3 Desarrollo específico de la contribución · 4 Postmortem · 5 Referencias, con anexos por letra. **Nota operativa importante:** el `.docx` NO se puede editar mientras esté abierto en Word (bloquea el archivo); cerrarlo antes de pedir cambios.

- **2026-07-04 · [COWORK]** — **Reestructurada la memoria a la estructura oficial** (Tipo Desarrollo): 1 Introducción · 2 GDD de alto nivel · 3 Desarrollo específico de la contribución · 4 Postmortem y conclusiones · 5 Referencias bibliográficas (página vacía con marcador, pendiente de redactar) · Anexo A Procedencia · Anexo B Encuestas · Anexo C GDD completo. Movido el GDD comprimido delante del Desarrollo; Procedencia/Encuestas convertidos en anexos; el GDD completo pasa a Anexo C. 89 pp. Pendiente en pulido: numeración de apartados y de subapartados de anexos; y el bloque «Plan de Acción y Objetivos Futuros» sigue dentro de Desarrollo (su contenido irá a «líneas futuras» del Postmortem).

- **2026-07-04 · [COWORK]** — Creada la copia de trabajo `TFG Predeposito_en desarrollo.docx` (88 pp, a partir del documento con el GDD anexado) para no mezclarla con `plantilla TFG.docx`. De aquí en adelante las ediciones de la memoria van sobre este archivo.

- **2026-07-04 · [COWORK]** — Insertado el **GDD completo (52 pp) como Anexo A** en `plantilla TFG.docx` para cumplir el requisito de extensión de la rúbrica: el documento pasa de 21 a **88 páginas**. Se mantiene el GDD de alto nivel en el cuerpo (rúbrica: «en su justa medida»). Backup previo en `plantilla TFG (backup antes de anexar GDD).docx`. Pendiente para el pulido: evitar que el índice general absorba los encabezados del GDD y rellenar los *placeholders* de cabecera (nombre y título del TFE).

- **2026-06-30 · [COWORK]** — Arrancada la preparación de la entrega final. Decisión de alcance de Mario: se cierra con el estado actual del juego; lo pendiente (PvP, arte final, nivel final, diploma…) pasa a «líneas futuras» en el Postmortem. Redactado un primer borrador completo del apartado «Desarrollo específico de la contribución» (a partir del inventario de sistemas), pendiente de revisión de Mario antes de integrarlo en `plantilla TFG.docx`. Establecida la lista de tareas del cierre (memoria + build + tráiler).

- **2026-06-30 · [CLAUDE CODE]** — Respondida la petición de Cowork: **inventario de la contribución por sistemas** (52 scripts de juego en `Assets/Yo/assets/scripts/`; las herramientas de editor son utilidades internas que no van en la build). Estado por sistema:

  1. **Backend y sincronización (Firebase)** — *Completo.* Acceso (Authentication) y base de datos en tiempo real (Firestore); capa central de datos de la que dependen todos los sistemas. → `FirebaseManager`, `AulaDataManager`, `LoginAlumnoUI`, `NewLoginManager`
  2. **Gestión de aula y sesión** — *Completo.* El profesor crea el aula con código; control del inicio de combate con sala de espera; ajustes y cierre de sesión. → `ControlCombateProfesor`, `AjustesUI`
  3. **Mapa de planetas y niebla de guerra** — *Parcial.* Spawn de planetas con modelo low-poly aleatorio persistido, selección, niebla de guerra (bloqueo/revelado por flota), borrado con recolocación y transición de cámara. *Pendiente:* progresión semi-lineal y nivel final opcional. → `PlanetSpawner`, `PlanetSelectable`, `PlanetSelectionManager`, `CameraTransition`
  4. **Sistema de preguntas (editor)** — *Completo.* Crear/editar/borrar preguntas por planeta (opciones, tiempo, puntos), persistidas en Firebase. *(Nota: corregido un bug de layout que solo aparecía en build —ScrollRect con Content nulo—; pendiente de verificar en una build nueva.)* → `EditorPreguntasUI`, `CreadorPreguntas`, `Pregunta`
  5. **Combate — asalto planetario** — *Parcial.* Núcleo por turnos basado en preguntas: roles atacante/defensor, energía y disparo a zonas, escudo de flota, ataques entrantes, estado sincronizado en Firebase con relevo automático del líder (failover), briefing y resultados. *Pendiente:* PvP entre flotas, planetas de evento y boost/escudo de líder. → `CombateAsaltoManager`, `EstadoFlotaCombate`, `EstrategiaAsaltoPlanetario`, `EstrategiaCombate`, `MotorPreguntas`, `SistemaCombateAlumno`, `ConfigCombateGlobal`, `ConfigCombatePlaneta`, `ZonaPlaneta`, `AtaqueEntrante`, `TipoCombate`, `SensorPanelEspera`, `PantallaBriefing`, `PantallaResultados`
  6. **HUD de combate** — *Completo.* Interfaz compartida + vistas de atacante y defensor (zonas, ataques entrantes, escudo/vida). → `HUDCombateCompartido`, `HUDCombateAtacante`, `HUDCombateDefensor`, `FilaZonaHUDCompartido`, `BotonZonaAtaque`, `TarjetaAtaqueEntrante`
  7. **Editor de combate (zonas del planeta)** — *Completo.* Configuración de las zonas de vida de cada planeta. → `EditorCombatePlaneta`, `FilaZonaEditor`
  8. **Gestión de flotas** — *Completo.* Crear flotas (máx. 6), asignar alumnos (máx. 10/flota), designar líder, disolver; vista del alumno y estrellas de flota. → `PanelControlFlotas`, `FlotaUI`, `Flota`, `FlotaMemberRow`, `ListaAlumnosUI`, `PantallaFlotaAlumnoUI`
  9. **Progresión, perfil y logros** — *Parcial.* XP, niveles, rangos, insignias automáticas, medallas del profesor, perfil personalizable, historial de sesiones, progreso global y rankings. *Pendiente:* diploma final. → `PerfilAlumnoUI`, `ProgresoGlobalUI`, `HistorialProfesorUI`
  10. **Mensajería** — *Completo.* Mensajes profesor→alumno en tiempo real con badge de notificación. → `PanelMensajesProfesor`, `PanelMensajesAlumno`
  11. **UI transversal y audio** — *Completo.* Sistema de audio (auto-detección de botones, música/SFX), avisos (Toast), diálogos de confirmación y control de volumen. → `AudioManager`, `AudioManagerScene`, `SintetizadorAudio`, `ControlAudioUI`, `Toast`, `ConfirmDialog`

  *Herramientas de editor (internas, no van en la build):* `CrearBotonBorrarPlaneta`, `ArreglarBotonBorrar`, `DiagnosticarBotonBorrar`, `ArreglarScrollRectsRotos`.

- **2026-06-30 · [COWORK]** — Creado este archivo de coordinación. Verificada la versión canónica del documento TFG (`plantilla TFG.docx`, 8 jun: misma estructura que la de abril, sección 2 más desarrollada). Realizado el análisis de huecos de la memoria frente a la estructura oficial de UNIR (Tipo Desarrollo) y a 3 TFGs de referencia. Próximo paso: editar la memoria apartado por apartado con Mario.
