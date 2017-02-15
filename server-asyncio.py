#!/usr/bin/env python3

import xwiimote
from time import sleep
import struct
import asyncio
import functools
import websockets
import socket
import signal
import serverutils

# watched wiimotes
wiimotes = []
connections = []
udpSock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)

def print_wiimote(dev):
    print("syspath:" + dev.get_syspath())
    fd = dev.get_fd()
    print("fd:", fd)

def read_monitor(mon, wiimotes):
    # For once this PC is too fast, can't open the interface until the device
    # has settled
    # 0 gives no error for open, events come through fine but no keys
    # 1/4.0 gives 13 permission denied
    # 1/2.0 works fine it seems, might want to crank it up incase of errors
    sleep(1/2.0)
    newmotes = []
    wiimote_path = mon.poll()
    while wiimote_path is not None:
        try:
            dev = xwiimote.iface(wiimote_path)
            wiimotes.append(dev)
            # enable rumble and IR
            dev.open(dev.available() | xwiimote.IFACE_WRITABLE | xwiimote.IFACE_IR )
            newmotes.append(dev)
        except IOError as eo:
            print("Fail on creating the wiimote (", eo, ")")
        wiimote_path = mon.poll()
    return newmotes

def remove_device(wiimotes, dev):
    if dev in wiimotes:
        wiimotes.remove(dev)
        # Do i need to specify IR here?
        dev.close(xwiimote.IFACE_WRITABLE  | xwiimote.IFACE_IR )

handled_events = {
    xwiimote.EVENT_IR,
    xwiimote.EVENT_ACCEL,
    xwiimote.EVENT_KEY,
    xwiimote.EVENT_GONE
}

def send_udp(message):
    for websocket in connections:
        if websocket.open:
            udpSock.sendto(message,(websocket.remote_address[0],9001))

def send_ws(message):
    for websocket in connections:
        if websocket.open:
            asyncio.ensure_future(websocket.send(message))

def wiimote_event(dev):
    event = xwiimote.event()
    try:
        dev.dispatch(event)
    except BlockingIOError:
        return

    if event.type not in handled_events:
        return

    message = bytearray(struct.pack("<?i",False,dev.get_fd()))
    message.extend(serverutils.xwiimote_event_to_bytearray(event))
    message_bytes = bytes(message)


    if event.type == xwiimote.EVENT_ACCEL or event.type == xwiimote.EVENT_MOTION_PLUS or event.type == xwiimote.EVENT_IR:
        send_udp(message_bytes)
    else:
        send_ws(message_bytes)

    if event.type == xwiimote.EVENT_GONE:
        remove_device(wiimotes,dev)


def _wiimote_monitor_event(mon,loop):
    newmotes = read_monitor(mon, wiimotes)

    for dev in newmotes:
        loop.add_reader(dev.get_fd(), functools.partial(wiimote_event, dev))

        for websocket in connections:
            asyncio.ensure_future(websocket.send(struct.pack("<?i",True,dev.get_fd())))


async def handle(websocket,path):
    connections.append(websocket)
    print("Client connected")

    for dev in wiimotes:
        asyncio.ensure_future(websocket.send(struct.pack("<?i",True,dev.get_fd())))

    try:
        while True:
            message = await websocket.recv()

            if type(message) == bytes:
                message_type = struct.unpack("<i",message[:4])[0]

                # rumble
                if message_type == 1:
                    fd, state = struct.unpack_from("<ii",message[4:16])

                    for dev in wiimotes:
                        if dev.get_fd() == fd:
                            dev.rumble(state == 1)

            elif message is str:
                print("Got a s message" + message)

    except websockets.exceptions.ConnectionClosed as e:
        print("Client disconnected")
        connections.remove(websocket)

try:
    mon = xwiimote.monitor(True, True)
except SystemError as e:
    print("ooops, cannot create monitor (", e, ")")
    exit(1)

loop = asyncio.get_event_loop()

wiimote_monitor_event = functools.partial(_wiimote_monitor_event,mon,loop)

# get current wiimotes
wiimote_monitor_event()


print("Starting websocket server")
start_server = websockets.serve(handle, '', 9000)
server_task = loop.run_until_complete(start_server)
print("Websocket server started")

# def sigint_handler():
#     print('Stopping')
#
#     server_task.close()
#
#     for task in asyncio.Task.all_tasks():
#         task.cancel()
#
# loop.add_signal_handler(signal.SIGINT, sigint_handler)


print("Monitoring wiimote events")
loop.add_reader(mon.get_fd(False), wiimote_monitor_event)

print("Starting event loop")
loop.run_forever()

# cleaning
for dev in wiimotes:
    remove_device(wiimotes, dev)
exit(0)
