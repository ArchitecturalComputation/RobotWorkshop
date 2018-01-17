MODULE AutoPickAndPlace

    RECORD Block
        num info;
        pose pick;
        pose place;
    ENDRECORD

    PERS tooldata gripper:=[TRUE,[[142.500,0.000,32.500],[0.5000,0.5000,-0.5000,-0.5000]],[2.0,[142.500,0.000,32.500],[1,0,0,0],0,0,0]];
    TASK PERS wobjdata frame:=[FALSE,TRUE,"",[[504.290,681.790,193.620],[-0.6969,0.0012,0.0021,0.7172]],[[0,0,0],[1,0,0,0]]];

    ! PERS string IP:="192.168.0.3";
    PERS string IP:="127.0.0.1";
    PERS num port:=1025;
    VAR socketdev socket;
    VAR extjoint exj:=[9E9,9E9,9E9,9E9,9E9,9E9];


    PROC main()
        ConfL\Off;

        MoveAbsJ [[0.000,0.000,18.000,0.000,-18.000,0.000],exj],v500,fine,gripper\WObj:=frame;
        PulseDO DO10_2;

        ClientConnect;

        WHILE TRUE DO
            PickAndPlace(GetBlock());
        ENDWHILE
    ENDPROC


    PROC ClientConnect()
        SocketClose socket;
        SocketCreate socket;
        SocketConnect socket,IP,port;
    ERROR
        RETRY;
    ENDPROC


    FUNC Block GetBlock()
        CONST num byteCount:=15;
        VAR rawbytes sendBytes;
        VAR rawbytes getBytes;
        VAR Block outBlock;
        VAR num floats{byteCount-1};

        PackRawBytes 1,sendBytes,1\IntX:=DINT;
        SocketSend socket\RawData:=sendBytes;
        SocketReceive socket\RawData:=getBytes\ReadNoOfBytes:=15*4;
        UnpackRawBytes getBytes,1,outBlock.info\IntX:=DINT;

        IF outBlock.info=0 THEN
            TPWrite "Info: "\Num:=outBlock.info;
            Stop;
        ENDIF

        FOR i FROM 1 TO byteCount-1 DO
            UnpackRawBytes getBytes,i*4+1,floats{i}\Float4;
        ENDFOR

        outBlock.pick:=[[floats{1},floats{2},floats{3}],[floats{4},floats{5},floats{6},floats{7}]];
        outBlock.place:=[[floats{8},floats{9},floats{10}],[floats{11},floats{12},floats{13},floats{14}]];

        TPWrite "Info: "\Num:=outBlock.info;
        TPWrite "Block pick pos: "\Pos:=outBlock.pick.trans;
        TPWrite "Block pick rot: "\Orient:=outBlock.pick.rot;

        RETURN outBlock;

        !ERROR
        !    ClientConnect;
        !    RETRY;
    ENDFUNC


    PROC PickAndPlace(Block inBlock)
        VAR robtarget pick;
        VAR robtarget place;
        VAR robtarget pickNeutral;
        VAR robtarget placeNeutral;
        VAR pos currentPos;
        VAR num offset:=45+20;
        VAR orient neutral:=[1,0,0,0];
        VAR speeddata speed1:=v200;
        VAR speeddata speed2:=v500;
        VAR speeddata speed3:=v1000;
        VAR zonedata zone1:=fine;
        VAR zonedata zone2:=z1;
        VAR zonedata zone3:=z15;

        currentPos:=CPos(\Tool:=gripper\WObj:=frame);

        pick:=[inBlock.pick.trans,inBlock.pick.rot,[1,1,0,0],exj];
        place:=[inBlock.place.trans,inBlock.place.rot,[1,1,0,0],exj];

        pickNeutral:=[inBlock.pick.trans,neutral,[1,1,0,0],exj];
        placeNeutral:=[inBlock.place.trans,neutral,[1,1,0,0],exj];

        ! pick avoid diagonal movements
        IF currentPos.z>(pick.trans.z+offset) THEN
            MoveL [[pick.trans.x,pick.trans.y,currentPos.z+offset],neutral,[1,1,0,0],exj],speed3,zone3,gripper\WObj:=frame;
        ELSE
            MoveL [[currentPos.x,currentPos.y,pick.trans.z+offset+offset],neutral,[1,1,0,0],exj],speed3,zone3,gripper\WObj:=frame;
        ENDIF

        ! pick offset neutral       
        MoveL Offs(pickNeutral,0,0,offset),speed3,zone3,gripper\WObj:=frame;

        ! pick offset aligned       
        MoveL Offs(pick,0,0,offset),speed3,zone3,gripper\WObj:=frame;

        ! pick offset grip       
        MoveL Offs(pick,0,0,0),speed1,zone1,gripper\WObj:=frame;
        PulseDO DO10_1;
        WaitTime 0.25;

        ! pick offset aligned loaded       
        MoveL Offs(pick,0,0,offset),speed2,zone3,gripper\WObj:=frame;

        ! pick offset neutral loaded       
        MoveL Offs(pickNeutral,0,0,offset),speed3,zone3,gripper\WObj:=frame;

        ! place avoid diagonal movements
        IF pick.trans.z>place.trans.z THEN
            MoveL [[place.trans.x,place.trans.y,pick.trans.z+offset],neutral,[1,1,0,0],exj],speed3,zone3,gripper\WObj:=frame;
        ELSE
            MoveL [[pick.trans.x,pick.trans.y,place.trans.z+offset],neutral,[1,1,0,0],exj],speed3,zone3,gripper\WObj:=frame;
        ENDIF

        ! place offset neutral       
        MoveL Offs(placeNeutral,0,0,offset),speed3,zone3,gripper\WObj:=frame;

        ! place offset aligned       
        MoveL Offs(place,0,0,offset),speed3,zone3,gripper\WObj:=frame;

        ! place offset grip       
        MoveL Offs(place,0,0,0),speed1,zone1,gripper\WObj:=frame;
        PulseDO DO10_2;
        WaitTime 0.25;

        ! place offset aligned loaded       
        MoveL Offs(place,0,0,offset),speed2,zone3,gripper\WObj:=frame;

        ! place offset neutral loaded       
        MoveL Offs(placeNeutral,0,0,offset),speed3,zone3,gripper\WObj:=frame;

        ! retract for camera
        IF inBlock.info=2 THEN
            MoveL [[place.trans.x,100,place.trans.z+offset],neutral,[1,1,0,0],exj],speed3,zone3,gripper\WObj:=frame;
        ENDIF

    ENDPROC

ENDMODULE