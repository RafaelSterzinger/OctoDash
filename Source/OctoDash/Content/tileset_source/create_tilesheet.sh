#!/bin/bash

if [[ $# -eq 0 ]] ; then
    echo 'usage: ./create_tilesheet.sh <folder with folders of PNGs>'
    exit 0
fi

convert $1/*/*.PNG +append spritesheet.PNG
