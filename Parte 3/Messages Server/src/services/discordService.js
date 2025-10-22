const { Client, GatewayIntentBits, Partials } = require('discord.js');
const fs = require('fs');
const logger = require('../utils/logger');

const TOKEN = process.env.DISCORD_BOT_TOKEN;

if (!TOKEN) {
  logger.error('Falta la variable DISCORD_BOT_TOKEN en .env');
  process.exit(1);
}

// Solo los intents necesarios para enviar mensajes y DMs
const client = new Client({
  intents: [
    GatewayIntentBits.Guilds,
    GatewayIntentBits.DirectMessages
  ],
  partials: [Partials.Channel] 
});

// Evento cuando el bot se conecta correctamente
client.once('ready', () => {
  logger.info(`Bot de Discord conectado como ${client.user.tag}`);
});

// Iniciar sesión con el token del bot
client.login(TOKEN);


async function sendFile({ userId, fileBuffer }) {
  try {
    // Buscar al usuario por su ID
    const user = await client.users.fetch(userId);
    if (!user) throw new Error('No se pudo encontrar el usuario en Discord');

    // Crear un archivo temporal local
    const tempPath = `./Orders_${Date.now()}.pdf`;
    fs.writeFileSync(tempPath, fileBuffer);

    // Enviar el mensaje con el archivo adjunto
    await user.send({
      content: `Estimado ${user.tag} aqui tiene el documento solicitado en formato pdf`,
      files: [tempPath]
    });

    // Eliminar el archivo temporal después del envío
    fs.unlinkSync(tempPath);

    logger.info(`Archivo enviado correctamente a ${user.tag}`, { userId });
    return { success: true, userId };
  } catch (err) {
    // Manejo de errores
    logger.error('Error enviando archivo por DM', { error: err.message });
    throw { status: 502, message: 'Error enviando archivo por DM' };
  }
}

module.exports = { sendFile };
