http = require('http');
fs = require('fs');
var spawn = require("child_process").spawn;

server = http.createServer( function(req, res) {

    console.dir(req.param);

    if (req.method == 'POST') {
        console.log("POST");
        var body = '';
        req.on('data', function (data) {
            body += data;
        });
        req.on('end', function () {
            console.log("Body: " + body);
                fs.writeFile("/home/ec2-user/server/payload.json", body, function(err) {
                        if(err) {
                                return console.log(err);
                        }
                        console.log("Wrote file.");
                        var pythonProcess = spawn('python',["/home/ec2-user/server/analytics.py"]);
                        pythonProcess.stdout.on('data', function (data){
                                console.log(data.toString());
                                res.end(data);
                                res.writeHead(200, {'Content-Type': 'text/html'});
                        });
                });
        });
    }
    else
    {
        console.log("GET");
        var html = "HEY";
        res.writeHead(200, {'Content-Type': 'text/html'});
        res.end(html);
    }

});

port = 3000;
server.listen(port, (err) => {
  if (err) {
    return console.log('something bad happened', err)
  }

  console.log(`server is listening on ${port}`)
})