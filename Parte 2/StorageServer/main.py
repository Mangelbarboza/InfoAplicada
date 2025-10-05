from fastapi import FastAPI, UploadFile, File, HTTPException, Form
from fastapi.responses import FileResponse
import os
from datetime import datetime
import shutil

app = FastAPI()
STORAGE_DIR = "storage"

# Asegura que el directorio de almacenamiento exista al iniciar la aplicación
os.makedirs(STORAGE_DIR, exist_ok=True)

@app.post("/api/storage/upload")
async def upload_file(
    correlationId: str = Form(...),
    clientId: str = Form(...),
    fileName: str = Form(...),
    file: UploadFile = File(...)
):
    try:
        # Crear el directorio de almacenamiento por fecha (YYYY-MM-DD)
        today = datetime.now().strftime('%Y-%m-%d')
        storage_path = os.path.join(STORAGE_DIR, today)
        os.makedirs(storage_path, exist_ok=True)

        # Guardar el archivo en la ruta con el nombre original
        file_path = os.path.join(storage_path, file.filename)
        with open(file_path, "wb") as buffer:
            shutil.copyfileobj(file.file, buffer)

        # Log de la operación (temporalmente en consola)
        print(f"Log: Archivo '{fileName}' con Correlation ID '{correlationId}' subido a las {datetime.now()}")

        return {"message": "File uploaded successfully", "correlationId": correlationId}
    except Exception as e:
        print(f"Error uploading file: {e}")
        raise HTTPException(status_code=500, detail="Error uploading file")

@app.get("/api/storage/file/{correlationId}")
async def get_file(correlationId: str):
    # Buscar el archivo en los subdirectorios
    for root, _, files in os.walk(STORAGE_DIR):
        for file in files:
           
            # Por ejemplo: '12345_reporte.pdf'.
            if file.startswith(f"{correlationId}_") and file.endswith(".pdf"):
                file_path = os.path.join(root, file)
                # Log de la operación (temporalmente en consola)
                print(f"Log: Archivo con Correlation ID '{correlationId}' recuperado a las {datetime.now()}")
                # Devuelve el archivo como una respuesta de tipo FileResponse
                return FileResponse(file_path, media_type="application/pdf", filename=file)

    # Si no se encuentra el archivo, se lanza una excepción HTTPException
    raise HTTPException(status_code=404, detail="File not found")