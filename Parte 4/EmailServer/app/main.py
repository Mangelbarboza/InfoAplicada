# app/main.py
import logging
from fastapi import FastAPI
from app.routers import email as email_router
from app.routers import test_fetch as test_router

logging.basicConfig(level=logging.INFO)

app = FastAPI(title="Email Service")
app.include_router(email_router.router)
app.include_router(test_router.router)