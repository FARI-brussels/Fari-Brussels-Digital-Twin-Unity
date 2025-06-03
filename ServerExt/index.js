const express = require("express");
const fs = require("fs");
const path = require("path");
const port = process.env.PORT || 5000;



const app = express();

app.use('/api-test', (req, res, next) => {
    res.setHeader('Content-Type', 'application/json');
    res.setHeader('Access-Control-Allow-Origin', '*');
    next();
});

app.get("/",(req,res) => {
    res.send("Api for DeLijn data");
})

app.listen(port, () => {
    console.log(`Serveur Online on port ${port}`);
    console.log(`Endpoints :`);
    console.log(`- http://localhost:${port}/api-test/routes.json`);
    console.log(`- http://localhost:${port}/api-test/shapes.json`);
    console.log(`- http://localhost:${port}/api-test/stops.json`);
    console.log(`- http://localhost:${port}/api-test/trips.json`);
});

app.get("/api-test/routes.json", (req, res) => {
    try {
        const filePath = path.join(__dirname, 'geoJson/routes.json'); 
        const data = fs.readFileSync(filePath, 'utf8');
        res.json(JSON.parse(data));
    } catch (error) {
        console.error('Error of reading routes.json:', error);
        res.status(404).json({ error: 'Files not found' });
    }
});


app.get("/api-test/stops.json", (req, res) => {
    try {
        const filePath = path.join(__dirname, 'geoJson/stops.json'); 
        const data = fs.readFileSync(filePath, 'utf8');
        res.json(JSON.parse(data));
    } catch (error) {
        console.error('Error of reading stops.json:', error);
        res.status(404).json({ error: 'Files not found' });
    }
});


app.get("/api-test/shapes.json", (req, res) => {
    try {
        const filePath = path.join(__dirname, 'geoJson/shapes.json');
        
        // Vérifier si le fichier existe
        if (!fs.existsSync(filePath)) {
            return res.status(404).json({ error: 'File not found' });
        }

        // Obtenir la taille du fichier
        const stat = fs.statSync(filePath);
        const fileSize = stat.size;
        
        console.log(`Serving shapes.json (${Math.round(fileSize / 1024 / 1024)}MB)...`);
        
        // Headers pour le streaming
        res.setHeader('Content-Length', fileSize);
        res.setHeader('Content-Type', 'application/json');
        
        // Créer un stream de lecture
        const readStream = fs.createReadStream(filePath);
        
        // Gérer les erreurs du stream
        readStream.on('error', (error) => {
            console.error('Error streaming shapes.json:', error);
            if (!res.headersSent) {
                res.status(500).json({ error: 'Error reading file' });
            }
        });
        
        // Pipe le fichier vers la réponse
        readStream.pipe(res);
        
    } catch (error) {
        console.error('Error with shapes.json:', error);
        res.status(500).json({ error: 'Internal server error' });
    }
});

app.get("/api-test/trips.json", (req, res) => {
    try {
        const filePath = path.join(__dirname, 'geoJson/trips.json');
        
        // Vérifier si le fichier existe
        if (!fs.existsSync(filePath)) {
            return res.status(404).json({ error: 'File not found' });
        }

        // Obtenir la taille du fichier
        const stat = fs.statSync(filePath);
        const fileSize = stat.size;
        
        console.log(`Serving trips.json (${Math.round(fileSize / 1024 / 1024)}MB)...`);
        
        // Headers pour le streaming
        res.setHeader('Content-Length', fileSize);
        res.setHeader('Content-Type', 'application/json');
        
        // Créer un stream de lecture
        const readStream = fs.createReadStream(filePath);
        
        // Gérer les erreurs du stream
        readStream.on('error', (error) => {
            console.error('Error streaming trips.json:', error);
            if (!res.headersSent) {
                res.status(500).json({ error: 'Error reading file' });
            }
        });
        
        // Pipe le fichier vers la réponse
        readStream.pipe(res);
        
    } catch (error) {
        console.error('Error with trips.json:', error);
        res.status(500).json({ error: 'Internal server error' });
    }
});

app.get("/api-test", (req, res) => {
    res.json({
        message: "API GeoJSON disponible",
        endpoints: [
            "/api-test/routes.json",
            "/api-test/shapes.json", 
            "/api-test/stops.json",
            "/api-test/trips.json"
        ]
    });
});