//
//  EventSystem.cpp
//  EventSystem
//
//  Created by Rob Drury on 9/17/14.
//  Copyright (c) 2014 Rob Drury. All rights reserved.
//

#include "EventSystem.h"

queue<EventInfo> EventSystem::_dispatchedEvents;
queue<EventListener<Event>> EventSystem::_listeners;

EventSystem::EventSystem()
{
}

//Clean up the Event Registry
EventSystem::~EventSystem()
{
    while(!_dispatchedEvents.empty())
    {
        EventInfo front = _dispatchedEvents.front();
    
        front.params.clear();
        delete front.szTypes;
    
        front.szTypes = NULL;
        _dispatchedEvents.pop();
    }
    
    while(!_listeners.empty())
    {
        EventListener<Event> front = _listeners.front();
        
        _listeners.pop();
    }
}

//Exectue dispatched events and remove them from the queue
void EventSystem::Update()
{
    if(_dispatchedEvents.size() <= 0)
        return;
    
    while(_dispatchedEvents.size() > 0)
    {
        for(int j = 0; j < _listeners.size(); j++)
        {
            if(_dispatchedEvents.front().Type != _listeners.front().Type)
                continue;
            
            _listeners.front()(_dispatchedEvents.front());
        }
        
        EventInfo front = _dispatchedEvents.front();
        
        front.params.clear();
        delete front.szTypes;
        
        front.szTypes = NULL;
        _dispatchedEvents.pop();
    }
}