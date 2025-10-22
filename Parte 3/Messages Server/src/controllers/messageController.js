const express = require('express');
const router = express.Router();
const storageService = require('../services/storageService');
const discordService = require('../services/discordService');
const logger = require('../utils/logger');
const LogMessageDTO = require('../models/LogMessageDTO');
const logMessage = require('../utils/logMessage');

router.post('/send', async (req, res) => {
  const { correlationId, recipientId } = req.body;

  if (!correlationId || !recipientId) {
    return res.status(400).json({ error: 'Faltan parámetros: correlationId y recipientId son obligatorios' });
  }

  try {
    // Obtener archivo desde el Storage Server
    const { buffer, contentType } = await storageService.getFile(correlationId);

    if (!buffer || !contentType.includes('pdf')) {
        const log = new LogMessageDTO({
        correlationId,
        service: 'Messaging Server',
        endpoint: '/api/messaging/send',
        payload: { error: 'Archivo no es PDF válido' },
        success: false
      });
      await logMessage.sendLog(log);
      return res.status(422).json({ error: 'El archivo recuperado no es un PDF válido' });
    }

    // Enviar el archivo por DM
    const result = await discordService.sendFile({
      userId: recipientId,
      fileBuffer: buffer,
    });

    const successLog = new LogMessageDTO({
      correlationId,
      service: 'Messaging Server',
      endpoint: '/api/messaging/send',
      payload: { UserId: recipientId,
                 Service: 'Discord'
      },
      success: true
    });
    await logMessage.sendLog(successLog);
    logger.info('Documento enviado por DM correctamente', { correlationId, recipientId });
    res.status(200).json({ success: true, result });
  } catch (err) {
    const errorLog = new LogMessageDTO({
      correlationId,
      service: 'Messaging Server',
      endpoint: '/api/messaging/send',
      payload: { error: err.message },
      success: false
    });
    await logMessage.sendLog(errorLog);
    logger.error('Error al enviar documento por DM', { error: err.message });
    res.status(err.status || 500).json({ error: err.message });
  }
});

module.exports = router;
