﻿namespace Nessos.Thespian.Tests

open System
open NUnit.Framework
open FsUnit

open Nessos.Thespian
open Nessos.Thespian.Tests.TestDefinitions

[<AbstractClass>]
type PrimaryProtocolTests(primaryProtocolFactory: IPrimaryProtocolFactory) =
  abstract PrimaryProtocolFactory: IPrimaryProtocolFactory
  default __.PrimaryProtocolFactory = primaryProtocolFactory

  [<TestFixtureSetUp>]
  member self.SetUp() =
    Actor.DefaultPrimaryProtocolFactory <- self.PrimaryProtocolFactory
  
  [<Test>]
  member __.``Primitive actor bind - actor name``() =
    let actor = new Actor<TestMessage<unit>>("testActorName", PrimitiveBehaviors.nill)

    actor.Name |> should equal "testActorName"

  [<Test>]
  member __.``Actor.bind primitive behavior - actor name``() =
    let actor = Actor.bind PrimitiveBehaviors.nill

    actor.Name |> should not' (equal String.Empty)


  [<Test>]
  member __.``Unpulished actor, ActorRef.Protocols size is 1``() =
    let actor = Actor.bind PrimitiveBehaviors.nill

    actor.Ref.Protocols.Length |> should equal 1

  [<Test>]
  member __.``ActorRef via property equals ActorRef via operator``() =
    let actor = Actor.bind PrimitiveBehaviors.nill

    let refByProperty = actor.Ref
    let refByOperator = !actor

    refByProperty |> should equal refByOperator

  [<Test>]
  member __.``New actor no pending messages``() =
    let actor = Actor.bind PrimitiveBehaviors.nill

    actor.PendingMessages |> should equal 0

  [<Test>]
  [<ExpectedException(typeof<ArgumentException>)>]
  member __.``Create actor with no protocol``() =
    let actor = new Actor<TestMessage<unit>>("unrealisableActor", Array.empty, PrimitiveBehaviors.nill)
    ()

  [<Test>]
  [<ExpectedException(typeof<ArgumentException>)>]
  member self.``Create actor with non-primitive actor protocol``() =
    let primary = self.PrimaryProtocolFactory.Create<TestMessage<unit>>("unrealisable")
    let primaryRef = new ActorRef<TestMessage<unit>>("unrealisable", [| primary.Client |])
    let tcpProtocol = new Remote.TcpProtocol.Bidirectional.ProtocolServer<TestMessage<unit>>("unrealisable", new System.Net.IPEndPoint(System.Net.IPAddress.Any, 0), primaryRef) :> IProtocolServer<_>
    let actor = new Actor<TestMessage<unit>>("unrealisable", [| tcpProtocol |], PrimitiveBehaviors.nill)
    ()

  [<Test>]
  [<ExpectedException(typeof<ArgumentException>)>]
  member self.``Create actor with name mismatch``() =
    let primary = self.PrimaryProtocolFactory.Create<TestMessage<unit>>("unrealisable") :> IProtocolServer<_>
    let actor = new Actor<TestMessage<unit>>("unrealisable'", [| primary |], PrimitiveBehaviors.nill)
    ()

  [<Test>]
  member __.``Actor.Name = ActorRef.Name after bind``() =
    let actor = Actor.bind PrimitiveBehaviors.nill
    actor.Name |> should equal actor.Ref.Name

  [<Test>]
  member __.``Actor.Name = ActorRef.Id.Name``() =
    let actor = Actor.bind PrimitiveBehaviors.nill
    actor.Name |> should equal actor.Ref.Id.Name

  [<Test>]
  member __.``Simple start/stop``() =
    let actor = Actor.bind PrimitiveBehaviors.nill
    actor.Start()
    actor.Stop()
    ()

  [<Test>]
  member __.``use binding on Actor.start``() =
    use actor = Actor.bind PrimitiveBehaviors.nill |> Actor.start in ()


  [<Test>]
  [<ExpectedException(typeof<ActorInactiveException>)>]
  member __.``Post to stopped Actor``() =
    let actor = Actor.bind PrimitiveBehaviors.nill

    !actor <-- TestAsync()

  [<Test>]
  member __.``Post to started Actor and then post to stopped``() =
    let actor = Actor.bind PrimitiveBehaviors.nill |> Actor.start

    !actor <-- TestAsync()

    actor.Stop()

    TestDelegate(fun () -> !actor <-- TestAsync()) |> should throw typeof<ActorInactiveException>

  [<Test>]
  [<ExpectedException(typeof<ActorInactiveException>)>]
  member __.``Post with reply to stopped actor``() =
    let actor = Actor.bind PrimitiveBehaviors.nill

    !actor <!= fun ch -> TestSync(ch, ())

  [<Test>]
  member __.``Post with reply to started Actor then post with reply to stopped``() =
    let actor = Actor.bind PrimitiveBehaviors.consumeOne |> Actor.start

    !actor <!= fun ch -> TestSync(ch, ())

    actor.Stop()

    TestDelegate(fun () -> !actor <!= fun ch -> TestSync(ch, ())) |> should throw typeof<ActorInactiveException>

  [<Test>]
  member __.``Post to started actor then post with reply to stopped``() =
    let actor = Actor.bind PrimitiveBehaviors.nill |> Actor.start

    !actor <-- TestAsync()

    actor.Stop()

    TestDelegate(fun () -> !actor <!= fun ch -> TestSync(ch, ())) |> should throw typeof<ActorInactiveException>

  [<Test>]
  member __.``Post with reply to started actor then post to stopped``() =
    let actor = Actor.bind PrimitiveBehaviors.consumeOne |> Actor.start

    !actor <!= fun ch -> TestSync(ch, ())

    actor.Stop()

    TestDelegate(fun () -> !actor <-- TestAsync()) |> should throw typeof<ActorInactiveException>

  [<Test>]
  member __.``Primitive behavior self stop``() =
    use actor = Actor.bind PrimitiveBehaviors.selfStop |> Actor.start

    !actor <!= fun ch -> TestSync(ch, ())

    TestDelegate(fun () -> !actor <-- TestAsync()) |> should throw typeof<ActorInactiveException>

  [<Test>]
  member __.``Self stop requires sync message``() =
    use actor = Actor.bind PrimitiveBehaviors.selfStop |> Actor.start

    !actor <-- TestAsync()

    try !actor <-- TestAsync() with :? ActorInactiveException -> ()

  [<Test>]
  member __.``Call start multiple times``() =
    use actor = Actor.bind PrimitiveBehaviors.nill |> Actor.start

    actor.Start()
    actor.Start()

    !actor <-- TestAsync()

    actor.Start()

    !actor <-- TestAsync()

  [<Test>]
  member __.``Call stop multiple times``() =
    use actor = Actor.bind PrimitiveBehaviors.nill |> Actor.start

    !actor <-- TestAsync()

    actor.Stop()
    actor.Stop()

    TestDelegate(fun () -> !actor <-- TestAsync()) |> should throw typeof<ActorInactiveException>

    actor.Stop()

    TestDelegate(fun () -> !actor <-- TestAsync()) |> should throw typeof<ActorInactiveException>

  [<Test>]
  member __.``Actor start/stop``() =
    use actor = Actor.bind PrimitiveBehaviors.nill |> Actor.start
    !actor <-- TestAsync()

    actor.Stop()
    TestDelegate(fun () -> !actor <-- TestAsync()) |> should throw typeof<ActorInactiveException>

    actor.Start()
    !actor <-- TestAsync()

    actor.Stop()
    TestDelegate(fun () -> !actor <-- TestAsync()) |> should throw typeof<ActorInactiveException>

  [<Test>]
  member __.``Rename actor``() =
    let actor = new Actor<TestMessage<unit>>("old", PrimitiveBehaviors.nill)

    actor.Name |> should equal "old"

    let actor' = actor.Rename("new")

    actor.Name |> should equal "old"

    actor'.Name |> should equal "new"

  [<Test>]
  member __.``Rename actor after start``() =
    use actor = Actor.bind PrimitiveBehaviors.nill |> Actor.start

    let actor' = Actor.rename "new" actor
    
    actor'.Name |> should equal "new"


  [<Test>]
  member __.``Rename actor gives different actors``() =
    use actor = new Actor<_>("old", PrimitiveBehaviors.stateful 0) |> Actor.start
    use actor' = actor |> Actor.rename "new" |> Actor.start

    actor.Ref |> should not' (equal actor'.Ref)

    !actor <-- TestAsync 42

    let s = !actor' <!= fun ch -> TestSync(ch, 4242)
    s |> should equal 0
    let s' = !actor <!= fun ch -> TestSync(ch, 0)
    s' |> should equal 42
    let s'' = !actor' <!= fun ch -> TestSync(ch, 0)
    s'' |> should equal 4242

  [<Test>]
  member __.``Actor.rename and start/stop``() =
    use actor = Actor.bind PrimitiveBehaviors.nill |> Actor.start
    use actor' = actor |> Actor.rename "new" |> Actor.start

    !actor <-- TestAsync()
    !actor' <-- TestAsync()

    actor.Stop()

    TestDelegate(fun () -> !actor <-- TestAsync()) |> should throw typeof<ActorInactiveException>
    !actor' <-- TestAsync()

    actor'.Stop()
    TestDelegate(fun () -> !actor <-- TestAsync()) |> should throw typeof<ActorInactiveException>
    TestDelegate(fun () -> !actor' <-- TestAsync()) |> should throw typeof<ActorInactiveException>

    actor.Start()
    !actor <-- TestAsync()
    TestDelegate(fun () -> !actor' <-- TestAsync()) |> should throw typeof<ActorInactiveException>

    actor'.Start()
    !actor <-- TestAsync()
    !actor' <-- TestAsync()

  [<Test>]
  member __.``Default actor name is guid string``() =
    use actor = Actor.bind PrimitiveBehaviors.nill

    let b, _ = Guid.TryParse(actor.Name)

    b |> should equal true


  [<Test>]
  member __.``Actor.PendingMessages = unprocessed messages``() =
    use actor = Actor.bind PrimitiveBehaviors.nill |> Actor.start
    actor.PendingMessages |> should equal 0

    !actor <-- TestAsync()
    actor.PendingMessages |> should equal 1

    !actor <-- TestAsync()
    actor.PendingMessages |> should equal 2

    !actor <-- TestAsync()
    actor.PendingMessages |> should equal 3

  [<Test>]
  member __.``Actor.PendingMessages and Actor.start/stop``() =
    use actor = Actor.bind PrimitiveBehaviors.nill |> Actor.start
    actor.PendingMessages |> should equal 0

    !actor <-- TestAsync()
    actor.PendingMessages |> should equal 1

    !actor <-- TestAsync()
    actor.PendingMessages |> should equal 2

    !actor <-- TestAsync()
    actor.PendingMessages |> should equal 3

    actor.Stop()
    actor.PendingMessages |> should equal 0

    actor.Start()
    !actor <-- TestAsync()
    actor.PendingMessages |> should equal 1

  [<Test>]
  member __.``Actor.ReBind``() =
    let actor = Actor.bind PrimitiveBehaviors.nill
    actor.ReBind(PrimitiveBehaviors.consume)
    use actor = Actor.start actor

    !actor <-- TestAsync()
    !actor <!= fun ch -> TestSync(ch, ())

  [<Test>]
  member __.``Actor.ReBind on started actor``() =
    use actor = Actor.bind (PrimitiveBehaviors.stateful 0) |> Actor.start
    !actor <-- TestAsync 42
    ignore (!actor <!= fun ch -> TestSync(ch, 4242))

    actor.ReBind(PrimitiveBehaviors.stateful 1)
    let s = !actor <!= fun ch -> TestSync(ch, 42)

    s |> should equal 1

  [<Test>]
  member __.``Actor.ReBind and Actor.PendingMessages``() =
    use actor = Actor.bind PrimitiveBehaviors.nill |> Actor.start
    !actor <-- TestAsync()
    !actor <-- TestAsync()
    actor.PendingMessages |> should equal 2

    actor.ReBind(PrimitiveBehaviors.nill)
    actor.PendingMessages |> should equal 0

  [<Test>]
  member __.``Actor clone constructor``() =
    use actor = Actor.bind PrimitiveBehaviors.nill |> Actor.rename "name"
    use actor' = new Actor<_>(actor)

    actor.Name |> should equal actor'.Name

  [<Test>]
  member __.``ActorRef.MessageType``() =
    use actor = Actor.bind PrimitiveBehaviors.nill
    actor.Ref.MessageType |> should equal typeof<TestMessage<unit>>

    use actor' = Actor.bind (PrimitiveBehaviors.stateful 42)
    actor'.Ref.MessageType |> should equal typeof<TestMessage<int, int>>

  [<Test>]
  member self.``ActorRef.Protocols``() =
    use actor = Actor.bind PrimitiveBehaviors.nill
    actor.Ref.Protocols.Length |> should equal 1
    let primaryProtocolName = self.PrimaryProtocolFactory.Create<unit>("test").ProtocolName
    actor.Ref.Protocols.[0] |> should equal primaryProtocolName

