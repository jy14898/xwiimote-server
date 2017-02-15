import xwiimote
import struct

# terribly named
'''
This implementation assumes that both sides are away of the size of ints.
So this will differ in things like longs being 8 bytes on 64
>> len(struct.pack(l,10)) == 8

[EventType][Time][KEY_OR_ABSDATA]
'''
def xwiimote_event_to_bytearray(event):
    result = bytearray()

    sec, usec = event.get_time()

    result.extend(struct.pack("<I",event.type))
    result.extend(struct.pack("<qq",sec,usec))

    # either send nothing
    # send xwii_event_key, which has [unsigned int code][unsigned int state]
    # send xwii_event_abs, which has [int32_t x, y, z]*(UP TO 8)
    # send xwii_event_abs, which has [int32_t x, y]*(4)

    # Only deal with these events for now
    if   event.type == xwiimote.EVENT_KEY:
        code, state = event.get_key()
        result.extend(struct.pack("<II",code,state))
    elif event.type == xwiimote.EVENT_ACCEL or event.type == xwiimote.EVENT_MOTION_PLUS:
        x, y, z = event.get_abs(0)
        result.extend(struct.pack("<iii",x,y,z))
    elif event.type == xwiimote.EVENT_IR:
        for i in [0, 1, 2, 3]:
            x, y, __ = event.get_abs(i)
            result.extend(struct.pack("<ii",x,y))

    return result
