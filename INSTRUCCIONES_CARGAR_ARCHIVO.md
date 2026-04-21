# Instrucciones para cargar el archivo de estudiantes

## Archivo de ejemplo creado
**Nombre del archivo:** `asignaciones_estudiantes_grado_grupo.csv`

## Pasos para usar el archivo:

### Opción 1: Usar el CSV directamente
1. Abre el archivo `asignaciones_estudiantes_grado_grupo.csv` con un editor de texto (Bloc de notas, Notepad++, etc.)
2. Verifica que la primera línea tenga exactamente estos encabezados (sin espacios adicionales):
   ```
   ESTUDIANTE (EMAIL),NOMBRE,APELLIDO,DOCUMENTO ID,FECHA NACIMIENTO,GRADO,GRUPO,JORNADA,INCLUSIÓN
   ```
3. Guarda el archivo como UTF-8
4. Abre Excel y usa "Abrir" → selecciona el archivo CSV
5. En el asistente de importación de Excel:
   - Selecciona "Delimitado"
   - Delimitador: **Coma (,)** ✅
   - Formato de archivo: **UTF-8** ✅
   - Click en "Finalizar"
6. Guarda como **.xlsx**
7. Carga el archivo .xlsx en `/StudentAssignment/Upload`

### Opción 2: Crear el archivo manualmente en Excel

1. Abre Excel y crea un nuevo libro
2. En la primera fila, escribe exactamente estos encabezados (en ese orden):
   - **ESTUDIANTE (EMAIL)**
   - **NOMBRE**
   - **APELLIDO**
   - **DOCUMENTO ID**
   - **FECHA NACIMIENTO**
   - **GRADO**
   - **GRUPO**
   - **JORNADA**
   - **INCLUSIÓN**

3. Asegúrate de que:
   - No haya espacios adicionales antes o después de los encabezados
   - Los encabezados estén exactamente en mayúsculas como se muestra arriba
   - La palabra "INCLUSIÓN" tenga la tilde (Ó)

4. Agrega tus datos de estudiantes en las filas siguientes
5. Guarda el archivo como **.xlsx**

## Formato de datos esperado:

- **ESTUDIANTE (EMAIL)**: Email válido (ejemplo: `juan.garcia@estudiante.com`)
- **NOMBRE**: Nombre del estudiante (ejemplo: `Juan`)
- **APELLIDO**: Apellido del estudiante (ejemplo: `García`)
- **DOCUMENTO ID**: Número de documento (ejemplo: `EST00011001`)
- **FECHA NACIMIENTO**: Formato DD/MM/YYYY (ejemplo: `15/03/2008`) o número de Excel
- **GRADO**: Nombre del grado (ejemplo: `6°`, `7°`, `8°`, `9°`, `10°`, `11°`)
- **GRUPO**: Nombre del grupo (ejemplo: `A`, `B`, `C`, `D`, `E`)
- **JORNADA**: Mañana, Tarde, o Noche (opcional, puede dejarse vacío)
- **INCLUSIÓN**: `si`, `no`, o dejar vacío (opcional)

## Ejemplo de una fila completa:

```
juan.garcia1@estudiante.com,Juan,García,EST00011001,15/03/2008,6°,A,Mañana,no
```

## Solución de problemas:

### Error: "Encabezados incorrectos"
- Verifica que los encabezados estén exactamente en el orden mostrado
- Asegúrate de que no haya espacios adicionales
- Verifica que "INCLUSIÓN" tenga la tilde (Ó) y esté en mayúsculas
- Si abriste el CSV en Excel, asegúrate de que la codificación sea UTF-8

### Error: "Faltan campos"
- Verifica que todas las filas tengan todos los campos requeridos
- Los campos opcionales (JORNADA e INCLUSIÓN) pueden dejarse vacíos

### Error: "Formato de fecha inválido"
- Usa el formato DD/MM/YYYY (ejemplo: `15/03/2008`)
- O usa el número de Excel (puedes formatear la celda como fecha)

