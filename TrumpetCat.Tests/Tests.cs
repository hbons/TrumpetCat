// Copyright (c) 2016 Hylke Bons
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.


using System;
using System.Threading;
using System.Threading.Tasks;

using TrumpetCat;
using TrumpetCat.Server;

class Tests
{
    static void Main (string[] args)
    {
        Task server_task = Task.Factory.StartNew (() => Server  ());
        Task client_listener_task = Task.Factory.StartNew (() => ClientListener ());
        Task client_sender_task = Task.Factory.StartNew (() => ClientSender ());

        Task.WaitAll (server_task, client_listener_task, client_sender_task);
    }


    static void Server ()
    {
        var server = new Server ();
        server.Start ();
    }


    static void ClientListener ()
    {
        Trumpet.Blowed += delegate (string song, string notes) {
            Console.WriteLine ("Trumpet blowed! ({0}, {1})", song, notes);
        };

        Trumpet.Listen ("kittens");
        Trumpet.Listen ("puppies");

        while (true)
            Thread.Sleep (1000);
    }


    static void ClientSender ()
    {
        while (true) {
            Thread.Sleep (1000);

            Trumpet.Blow ("kittens", "meow");
            Trumpet.Blow ("puppies", "woof");
            Trumpet.Blow ("puppies", "another woof");
        }
    }
}
