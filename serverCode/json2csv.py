import json

data = open("payload.json").read()
output = open("payload.csv", "w")
json_obj = json.loads(data)
string_builder = ''
for line in json_obj['payload']:
    if line == 'NULL':
        line = ''
    string_builder += str(line) + ","
string_builder = string_builder[:-1]
string_builder += ",false\n"
print(string_builder)
output.write(string_builder)