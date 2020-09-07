import sys
import time
import zmq

print("Python Started")

address = sys.argv[1]
arg = sys.argv[2]

context = zmq.Context()
socket = context.socket(zmq.REP)
address = str(address)
print("Python Connecting to Address " + address)
socket.bind(address)

print("Waiting for message " + str(arg) + " on " + str(address))
while True:
    try:
        #  Wait for next request from client
        message = socket.recv()
        decodedMessage = message.decode("utf-8")
        print(str(arg) +"Received request: %s" % message)

        if (decodedMessage  == "HELLO"):
            socket.send("READY".encode('utf-8'))
            print("Waiting for message " + str(arg) + " on " + str(address) + "sent READY")
        else:
            response = "K" + str(arg) + "|" + decodedMessage
            socket.send(response.encode('utf-8'))
    except Exception as e: 
        print("We had an error" + str(e))
        response = "E" + str(arg) + "|" + str(e)
        socket.send(response.encode('utf-8'))