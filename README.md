# xwiimote-server
Might also want https://github.com/dvdhrm/xwiimote/blob/master/res/50-xorg-fix-xwiimote.conf 

##INSTALL INSTRUCTIONS (for python bindings)

1. Go to [here](https://github.com/dvdhrm/xwiimote) to download the zip file containing the xwiimote software and unzip it in a suitable directory. Or clone using git.

2. Go to [here](https://github.com/dvdhrm/xwiimote-bindings) to download the zip file containing the xwiimote bindings and unzip it another directory. Or clone using git.

3. Install the following dependencies (ubuntu based distributions) via:

    `sudo apt-get install libudev-dev libncurses5-dev libncursesw5-dev autoconf autogen libtool swig python3-dev python3-tk python3-pip`

4. Install python modules:

    `sudo pip3 install pyudev pandas matplotlib`

5. Compile and install xwiimote library.
Change (cd) to xwiimote directory (xwiimote-master), then run:

    `sudo ./autogen.sh`
    
    `sudo ./configure`
    
    `sudo make`
    
    `sudo make install`
    

6. Create the xwiimote configure file in /etc/ld.so.comf.d:

    `cd /etc/ld.so.conf.d`
    
    `sudo nano xwiimote.conf`

    And add the following line to this file:

    `/usr/local/lib`

    and save: Ctrl O, Ctrl X. Next, reload library cache using the following:

    `sudo ldconfig`

7. Test xwiimote libraries are installed and can connect Wiiboard by connecting Wiiboard via Bluetooth and running:

    `sudo xwiishow`

    and follow instructions.

8. Compile and install xwiimote python bindings. Change (cd) to xwiimote-bindings directory and run following:

    `sudo ./autogen.sh`
    
    `sudo ./configure PYTHON=/usr/bin/python3`
    
    `sudo make`
    
    `sudo make install`
    

9. Add user to input group to allow user (‘usersname’) to run xwiimote software:

    `sudo usermod -a -G input usersname`

    then log out and log back in again

