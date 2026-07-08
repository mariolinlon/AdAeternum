# Ad Aeternum

**Videojuego educativo cooperativo** ambientado en una odisea espacial, desarrollado en Unity con backend en Firebase. El docente (Almirante) diseña retos académicos y supervisa el progreso en tiempo real, mientras los alumnos (Tripulantes), organizados en flotas, exploran un mapa con niebla de guerra resolviendo preguntas.

Proyecto de **Trabajo Fin de Grado** — Grado en Diseño y Desarrollo de Videojuegos, Universidad Internacional de La Rioja (UNIR).

## Autoría

- **Desarrollo:** Mario Fernández Martín.
- **Colaboración conceptual:** Félix González (gamificación e idea original) y Miguel Placín (perspectiva psicológica del diseño de interacciones).

## Tecnologías

- **Motor:** Unity (Universal Render Pipeline, URP).
- **Backend:** Firebase — Authentication (acceso de profesores y alumnos) y Cloud Firestore (base de datos en tiempo real).

## Cómo abrir el proyecto

1. Clonar el repositorio.
2. Abrirlo con **Unity Hub** (versión de Unity indicada en `ProjectSettings/ProjectVersion.txt`).
3. Importar el **Firebase Unity SDK** en el proyecto: los binarios nativos de Firebase (superiores a 100 MB) no se incluyen en el repositorio por el límite de tamaño de GitHub, y se restauran al importar el SDK.
4. Configurar tu propio proyecto de **Firebase** y añadir tus archivos de configuración (`google-services.json` / `google-services-desktop.json`) si no se incluyen en el repositorio.

## Características principales

- Autenticación y gestión de aulas en tiempo real.
- Mapa de planetas con niebla de guerra.
- Editor de preguntas configurable por el profesor.
- Sistema de combate por turnos basado en la resolución de preguntas, con relevo automático de líder (*failover*).
- Gestión de flotas, progresión, rangos, insignias, medallas y recompensas colectivas.
- Herramientas de seguimiento y análisis para el docente.

## Estado

Versión funcional (build ejecutable estable). Consulta la memoria del TFG y el Game Design Document para el detalle de diseño y desarrollo, y las líneas de trabajo futuras.
