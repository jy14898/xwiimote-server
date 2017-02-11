#!/usr/bin/env python3

import xwiimote
from time import sleep
import struct
import asyncio
import functools
import websockets

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
            wiimotes['all'].append(dev)
            # enable rumble and IR
            dev.open(dev.available() | xwiimote.IFACE_WRITABLE | xwiimote.IFACE_IR )
            newmotes.append(dev)
        except IOError as eo:
            print("Fail on creating the wiimote (", eo, ")")
        wiimote_path = mon.poll()
    return newmotes

def remove_device(wiimotes, dev):
    for devtype, devlist in wiimotes.items():
        if dev in devlist:
            devlist.remove(dev)
            dev.close(xwiimote.IFACE_WRITABLE)

# watched wiimotes
wiimotes = {'all':[]}
connections = []

try:
    mon = xwiimote.monitor(True, True)
except SystemError as e:
    print("ooops, cannot create monitor (", e, ")")
    exit(1)

loop = asyncio.get_event_loop()

def wiimote_event(dev):
    event = xwiimote.event()
    try:
        dev.dispatch(event)
    except BlockingIOError:
        return

    if event.type == xwiimote.EVENT_KEY:
        (code, state) = event.get_key()

        for websocket in connections:
            asyncio.ensure_future(websocket.send("KeyChange {} {} {}".format(dev.get_syspath(), code, state)))

    elif event.type == xwiimote.EVENT_GONE:
        loop.remove_reader(dev.get_fd())

        remove_device(wiimotes, dev)

        for websocket in connections:
            asyncio.ensure_future(websocket.send("Wiimote disconnected: {}".format(dev.get_syspath())))
    # elif event.type == xwiimote.EVENT_IR:
        # send over UDP

def wiimote_monitor_event():
    newmotes = read_monitor(mon, wiimotes)

    for dev in newmotes:
        loop.add_reader(dev.get_fd(), functools.partial(wiimote_event, dev))

        for websocket in connections:
            asyncio.ensure_future(websocket.send("Wiimote connected: {}".format(dev.get_syspath())))



async def handle(websocket,path):
    connections.append(websocket)
    while True:
        message = await websocket.recv()
        print("Got a message" + message)

wiimote_monitor_event()

start_server = websockets.serve(handle, '192.168.0.12', 9000)

loop.run_until_complete(start_server)
loop.add_reader(mon.get_fd(False), wiimote_monitor_event)

loop.run_forever()


# cleaning
for dev in wiimotes['all']:
    # p.unregister(dev.get_fd())
    remove_device(wiimotes, dev)
# p.unregister(mon_fd)
exit(0)
