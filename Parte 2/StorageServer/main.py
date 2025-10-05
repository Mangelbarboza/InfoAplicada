from fastapi import FastAPI, UploadFile, File, HTTPException, Form
from fastapi.responses import FileResponse
import os
from datetime import datetime
import shutil
import json

from kafka_service import KafkaLogProducer

KAFKA_BROKER = 'localhost:9092'
KAFKA_TOPIC = 'logs-storage'
kafka_producer = KafkaLogProducer(KAFKA_BROKER, KAFKA_TOPIC)

app = FastAPI()
STORAGE_DIR = "storage"
os.makedirs(STORAGE_DIR, exist_ok=True)

@app.post("/api/storage/upload")
async def upload_file(
    correlationId: str = Form(...),
    clientId: str = Form(...),
    fileName: str = Form(...),
    file: UploadFile = File(...)
):
    try:
        today = datetime.now().strftime('%Y-%m-%d')
        storage_path = os.path.join(STORAGE_DIR, today)
        os.makedirs(storage_path, exist_ok=True)

        file_path = os.path.join(storage_path, file.filename)
        with open(file_path, "wb") as buffer:
            shutil.copyfileobj(file.file, buffer)

        log_message = {
            "correlationId": correlationId,
            "service": "Storage Server",
            "endpoint": "/api/storage/upload",
            "timestamp": datetime.now().isoformat() + "Z",
            "payload": {
                "clientId": clientId,
                "fileName": fileName
            },
            "success": True
        }

        kafka_producer.send_log(log_message)

        return {"message": "File uploaded successfully", "correlationId": correlationId}
    except Exception as e:
        error_log = {
            "correlationId": correlationId,
            "service": "Storage Server",
            "endpoint": "/api/storage/upload",
            "timestamp": datetime.now().isoformat() + "Z",
            "payload": {
                "error": str(e)
            },
            "success": False
        }
        kafka_producer.send_log(error_log)
        
        raise HTTPException(status_code=500, detail="Error uploading file")

@app.get("/api/storage/file/{correlationId}")
async def get_file(correlationId: str):
    for root, _, files in os.walk(STORAGE_DIR):
        for file in files:
            if file.startswith(f"{correlationId}_") and file.endswith(".pdf"):
                file_path = os.path.join(root, file)
                
                log_message = {
                    "correlationId": correlationId,
                    "service": "Storage Server",
                    "endpoint": f"/api/storage/file/{correlationId}",
                    "timestamp": datetime.now().isoformat() + "Z",
                    "payload": {
                        "fileName": file
                    },
                    "success": True
                }
                kafka_producer.send_log(log_message)
                
                return FileResponse(file_path, media_type="application/pdf", filename=file)

    error_log = {
        "correlationId": correlationId,
        "service": "Storage Server",
        "endpoint": f"/api/storage/file/{correlationId}",
        "timestamp": datetime.now().isoformat() + "Z",
        "payload": {
            "error": "File not found"
        },
        "success": False
    }
    kafka_producer.send_log(error_log)
    
    raise HTTPException(status_code=404, detail="File not found")