const axios = require('axios');
const logger = require('../utils/logger');

const BASE_URL = process.env.STORAGE_SERVER_URL;

async function getFile(correlationId) {
  const url = `${BASE_URL}/api/storage/file/${encodeURIComponent(correlationId)}`;

  try {
    const response = await axios.get(url, { responseType: 'arraybuffer' });

    if (response.status !== 200) {
      throw new Error(`Storage Server devolvió estado ${response.status}`);
    }

    const contentType = response.headers['content-type'] || 'application/octet-stream';
    return { buffer: Buffer.from(response.data), contentType };
  } catch (err) {
    logger.error('Error obteniendo archivo del Storage Server', { error: err.message });
    throw { status: 502, message: 'Error comunicándose con el Storage Server' };
  }
}

module.exports = { getFile };
