"""
Note to future Christian, application collects timestamps from 4 hours in the future.

This code was used to change the stressed values inside of our dataset.

Enter two timestamps, and everything between those two would be changed to a stressed event instead of a nonstressed.
"""
timestamps = []
start = ""
quit = ""
while start != 'q':
    start = raw_input("Enter start time")
    quit = raw_input("Enter end time")
    if start == 'q':
        break
    timestamps.append((start,quit))

f = open("datanew.csv")
output = []
for line in f:
    out = 'false'
    values = line.split(", ")
    stamp = int(values[-1])
    for timestamp in timestamps:
        if stamp >= int(timestamp[0]) and stamp <= int(timestamp[1]):
             out = 'true'
    line = line.replace('\n', '')
    output.append(line + ', ' + out)
f = open("data.csv", 'w')
for line in output:
    f.write(line + '\n')
