const express = require('express');
const messageController = require('./controllers/messageController');

const app = express();
app.use(express.json());

app.use('/api/messaging', messageController);

app.get('/health', (req, res) => res.json({ status: 'ok' }));

module.exports = app;
