// Importar Express
const express = require('express');
const app = express();

// Puerto del servidor
const PORT = 3000;

// Middleware para manejar JSON
app.use(express.json());

// Ruta principal
app.get('/', (req, res) => {
  res.send('Servidor Express funcionando correctamente jajaja🚀');
});

// Iniciar el servidor
app.listen(PORT, () => {
  console.log(`Servidor escuchando en http://localhost:${PORT}`);
});
