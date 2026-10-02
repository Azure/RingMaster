#!/usr/bin/env bash

mkdir -p ~/Resources/Directory
export log=~/Resources/Directory/Startup.log

date >> $log

local_port_range=`sysctl -n net.ipv4.ip_local_port_range`
echo "Local Port Range (Start Port - End Port)" >> $log
echo $local_port_range >> $log

local_port_range=($local_port_range)
local_port_range=$((${local_port_range[1]} - ${local_port_range[0]}))

if [ $local_port_range -le 50000 ]; then
    echo "Fix dynamic port range" >> $log
    sysctl net.ipv4.ip_local_port_range="1025 65535"
fi
