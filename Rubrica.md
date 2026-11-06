## Fecha: miercoles 11 de noviembre de 2026 · Valor: 20 %

**Entregable de squad con demo en vivo.** El temario oficial lo exige de forma explicita:

> *"Implementacion, instalacion y **puesta en marcha de la solucion en un Smartphone**."*

## Rubrica

| Criterio | Peso |
|---|---|
| Instalada y corriendo en un **telefono fisico** | 25 % |
| Flujo completo: login → saldo → transferencia → confirmacion | 25 % |
| **Comportamiento offline**: encola y sincroniza al recuperar red | 30 % |
| **No duplica** la transferencia al reintentar | 20 % |

## El criterio que vale mas, y por que

El **30 % del comportamiento offline** y el **20 % de no-duplicacion** son el corazon de todo el semestre.

Una app movil vive con la red cayendose. Eso la convierte en el mismo problema que llevan seis semanas resolviendo:

| En el backend (Semanas 2-6) | En la app movil |
|---|---|
| Outbox: el evento no se pierde si el broker cae | Cola local: la transferencia no se pierde si no hay red |
| Idempotencia: procesar dos veces no duplica | Reintentar desde el celular no duplica la plata |
| Reintentos con espera exponencial | Reintentar sin tumbar la API al volver la senal |
| Consistencia eventual | La pantalla se pone al dia cuando vuelve la red |

**No es un tema nuevo. Es el mismo problema visto desde el otro lado del cable.**

## La demo: hay que provocar el fallo

No basta con mostrar que funciona con buena senal. En la sustentacion se pide **provocar el fallo en vivo**:

1. Iniciar una transferencia
2. **Activar modo avion a mitad de la operacion**
3. Mostrar que la app clsla encolo y le dijo la verdad al usuario
4. Desactivar modo avion
5. Mostrar que la transferencia se completa **una sola vez**

> Si al reactivar la red la transferencia se ejecuta dos veces, el criterio de no-duplicacion es cero. Ese es exactamente el defecto que encontramos en la Semana 5 del backend, ahora del lado del cliente.

## Requisitos

- [ ] APK instalado en un **telefono Android fisico** (basta **uno por squad**)
- [ ] Depuracion USB habilitada — verificarlo antes del 4 de noviembre
- [ ] Cola local persistente: si la app se cierra, la transferencia pendiente sobrevive
- [ ] Clave de idempotencia enviada desde el cliente
- [ ] La API accesible desde el telefono (misma red WiFi, o tunel)

## Si su squad no tiene telefono Android

Avisenlo **antes del 26 de octubre**, no el dia de la entrega. Se coordina prestamo entre squads o un dispositivo del laboratorio.

Referencia: [`docs/plan-y-evaluacion.md`](docs/plan-y-evaluacion.md)