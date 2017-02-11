#!/usr/bin/env python3

'''
So, each wiimote device gets a device descriptor, which we register a poller to
poll for events?
'''

'''
Plan is: UDP broadcast to find server/client
UDP to send accell/IR
TCP to send buttons/rumble
'''


import xwiimote
import select
import errno
from time import sleep
import socket
import struct

def print_wiimotes(wiimotes):
    for dev in wiimotes['all']:
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
    print_wiimotes(wiimotes)


p = select.poll()

# watched wiimotes
wiimotes = {'all':[]}

# 1 way socket for now
# can we listen to any UDP on the port? dont really care about who it came from
# for rumble

# Maybe we want two connections, UDP and TCP
# TCP for button presses, UDP for IR and ACCEL
# for now use UDP

sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
ip = input('IP: ')
port = int(input('PORT: '))
# sock.bind((ip,int(port)))

try:
    mon = xwiimote.monitor(True, True)
    newmotes = read_monitor(mon, wiimotes)
    for dev in newmotes:
        p.register(dev.get_fd(), select.POLLIN)

    print_wiimotes(wiimotes)
except SystemError as e:
    print("ooops, cannot create monitor (", e, ")")
    exit(1)

# register devices
mon_fd = mon.get_fd(False)
p.register(mon_fd, select.POLLIN)

# Precreate place to store the event we're checking when polling
revt = xwiimote.event()

# Test index
index = 0;

# When it comes to it, I believe we can poll UDP too
try:
    while True:
        polls = p.poll()

        # for all events
        for fd, evt in polls:

            # monitor
            if fd == mon_fd:
                newmotes = read_monitor(mon, wiimotes)
                for dev in newmotes:
                    p.register(dev.get_fd(), select.POLLIN)

                    # message = {
                    #     'type':'connect'
                    # }
                    #
                    # sock.sendto(bytes(json.dumps(message),'utf-8'),(ip,port));

                    if len(newmotes) > 0:
                        print_wiimotes(wiimotes)

            # wiimotes
            else:
                # To speed this up, we could use file descriptor as index?
                for dev in wiimotes['all']:
                    if fd == dev.get_fd():
                        try:
                            dev.dispatch(revt)

                            if revt.type == xwiimote.EVENT_GONE:
                                p.unregister(dev.get_fd())
                                remove_device(wiimotes, dev)

                                # message = {
                                #     'type':'disconnect'
                                # }
                                #
                                # sock.sendto(bytes(json.dumps(message),'utf-8'),(ip,port));

                            elif revt.type == xwiimote.EVENT_KEY:
                                (code, state) = revt.get_key()
                                print("Key:", code, ", State:", state)

                                # message = {
                                #     'type':'key',
                                #     'key':code,
                                #     'state':state
                                # }
                                #
                                # sock.sendto(bytes(json.dumps(message),'utf-8'),(ip,port));

                            elif revt.type == xwiimote.EVENT_IR:
                                for i in [0, 1, 2, 3]:
                                    if revt.ir_is_valid(i):
                                        x, y, z = revt.get_abs(i)
                                        sock.sendto(struct.pack("iiiiii",int(1),int(index),int(i),int(x),int(y),int(z)),(ip,port))
                                        index += 1
                                # message = {
                                #     'type':'ir'
                                # }
                                #
                                # for i in [0, 1, 2, 3]:
                                #     if revt.ir_is_valid(i):
                                #         x, y, z = revt.get_abs(i)
                                #         message['data'] = {
                                #             'i':i,
                                #             'x':x,
                                #             'y':y,
                                #             'z':z
                                #         }
                                #         print("IR", i, x, y, z)
                                #
                                #
                                # sock.sendto(bytes(json.dumps(message),'utf-8'),(ip,port));
                            #elif revt.type == xwiimote.EVENT_ACCEL:
                                # x, y, z = revt.get_abs(0)
                                # sock.sendto(struct.pack("iiiii",int(0),int(index),int(x),int(y),int(z)),(ip,port))
                                # index += 1
                        except IOError as e:
                            if e.errno != errno.EAGAIN:
                                print(e)
                                p.unregister(dev.get_fd())
                                remove_device(wiimotes, dev)
except KeyboardInterrupt:
    print("exiting...")

# cleaning
for dev in wiimotes['all']:
    p.unregister(dev.get_fd())
    remove_device(wiimotes, dev)
p.unregister(mon_fd)
exit(0)
