//
//  EventSystem.h
//  EventSystem
//
//  Created by Rob Drury on 9/17/14.
//  Copyright (c) 2014 Rob Drury. All rights reserved.
//

#ifndef EventSystem_EventSystem_h
#define EventSystem_EventSystem_h

#include <queue>
#include <vector>

#include "EventListener.h"
#include "EventInfo.h"
#include "EventType.h"
#include "Event.h"

using namespace std;

class EventSystem
{
public:
    EventSystem();
    ~EventSystem();
    
    void Update();
    
    //Register an event listener with the Event Registry
    static void RegisterListener(EventListener<Event> listener)
    {
        _listeners.push(listener);
    }
    
    //Dispatches an event to be executed on the next update
    static void DispatchEvent(EventType type, char* szTypes, ...)
    {
        EventInfo info;
        info.szTypes = szTypes;
        info.Type = type;
        
        va_list params;
        va_start(params, szTypes);
        
        int i = 0;
        
        while(szTypes[i] != '\0')
        {
            switch(szTypes[i])
            {
                case 'i':
                    info.Insert(va_arg(params, int*));
                    break;
                case 'f':
                    info.Insert(va_arg(params, float*));
                    break;
                case 'd':
                    info.Insert(va_arg(params, double*));
                    break;
                case 'c':
                case 's':
                    info.Insert(va_arg(params, char*));
                    break;
            }
            
            i++;
        }
        
        _dispatchedEvents.push(info);
    }
    
private:
    static queue<EventListener<Event>> _listeners;
    
    static queue<EventInfo> _dispatchedEvents;
};

#endif
