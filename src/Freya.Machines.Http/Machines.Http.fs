﻿module Freya.Machines.Http

open System
open Aether
open Aether.Operators
open Arachne.Http
open Arachne.Language
open Freya.Core
open Freya.Core.Operators
open Freya.Optics.Http
open Hephaestus

// Sunday!

// TODO: Full representation writer
// TODO: Module documentation
// TODO: Complete operations

// Later...?

// TODO: Determine correct ending for core
// TODO: Introduce a mechanism for logs, etc. (this might be more Freya.Core?)

(* Defaults

   Defaults which may be commonly used or applicable to multiple areas of
   functionality within the HTTP machine, such as default sets of allowed
   methods, etc. *)

[<RequireQualifiedAccess>]
module Defaults =

    let methodsAllowed =
        [ GET
          HEAD
          OPTIONS ]

(* Properties

   Properties of the resource which the machine represents, defined as a
   distinct set of types as these may be used/shared by any of the many
   elements defined as part of an HTTP machine. *)

[<RequireQualifiedAccess>]
module Properties =

    (* Types *)

    type private Properties =
        { Request: Request
          Representation: Representation
          Resource: Resource }

        static member request_ =
            (fun x -> x.Request), (fun r x -> { x with Request = r })


        static member representation_ =
            (fun x -> x.Representation), (fun r x -> { x with Representation = r })

        static member resource_ =
            (fun x -> x.Resource), (fun r x -> { x with Resource = r })

        static member empty =
            { Request = Request.empty
              Representation = Representation.empty
              Resource = Resource.empty }

     and private Request =
        { MethodsAllowed: Value<Method list> option }

        static member methodsAllowed_ =
            (fun x -> x.MethodsAllowed), (fun m x -> { x with MethodsAllowed = m })

        static member empty =
            { MethodsAllowed = None }

     and private Representation =
        { MediaTypesSupported: Value<MediaType list> option
          LanguagesSupported: Value<LanguageTag list> option
          CharsetsSupported: Value<Charset list> option
          ContentCodingsSupported: Value<ContentCoding list> option }

        static member mediaTypesSupported_ =
            (fun x -> x.MediaTypesSupported), (fun m x -> { x with MediaTypesSupported = m })

        static member languagesSupported_ =
            (fun x -> x.LanguagesSupported), (fun l x -> { x with LanguagesSupported = l })

        static member charsetsSupported_ =
            (fun x -> x.CharsetsSupported), (fun c x -> { x with CharsetsSupported = c })

        static member contentCodingsSupported_ =
            (fun x -> x.ContentCodingsSupported), (fun c x -> { x with ContentCodingsSupported = c })

        static member empty =
            { MediaTypesSupported = None
              LanguagesSupported = None
              CharsetsSupported = None
              ContentCodingsSupported = None }

     and private Resource =
        { ETags: Value<ETag list> option
          LastModified: Value<DateTime> option }

        static member eTags_ =
            (fun x -> x.ETags), (fun e x -> { x with ETags = e })

        static member lastModified_ =
            (fun x -> x.LastModified), (fun l x -> { x with LastModified = l })

        static member empty =
            { ETags = None
              LastModified = None }

    (* Optics *)

    let private properties_ =
            Configuration.element_ Properties.empty "properties"

    (* Request *)

    [<RequireQualifiedAccess>]
    [<CompilationRepresentation (CompilationRepresentationFlags.ModuleSuffix)>]
    module Request =

        let private request_ =
                properties_
            >-> Properties.request_

        let methodsAllowed_ =
                request_
            >-> Request.methodsAllowed_

    (* Representation *)

    [<RequireQualifiedAccess>]
    [<CompilationRepresentation (CompilationRepresentationFlags.ModuleSuffix)>]
    module Representation =

        let private representation_ =
                properties_
            >-> Properties.representation_

        let charsetsSupported_ =
                representation_
            >-> Representation.charsetsSupported_

        let contentCodingsSupported_ =
                representation_
            >-> Representation.contentCodingsSupported_

        let languagesSupported_ =
                representation_
            >-> Representation.languagesSupported_

        let mediaTypesSupported_ =
                representation_
            >-> Representation.mediaTypesSupported_

    (* Resource *)

    [<RequireQualifiedAccess>]
    [<CompilationRepresentation (CompilationRepresentationFlags.ModuleSuffix)>]
    module Resource =

        let private resource_ =
                properties_
            >-> Properties.resource_

        let eTags_ =
                resource_
            >-> Resource.eTags_

        let lastModified_ =
                resource_
            >-> Resource.lastModified_

(* Content

   Functionality for working with negotiated content, supporting both
   negotiation (used by the negotiation element of the HTTP model) and
   representation of a resource as part of an HTTP response (used throughout
   the HTTP model as part of the defined Handler type signature). *)

[<RequireQualifiedAccess>]
module Content =

    (* Types
    
       Types relating to content based operations, particularly around the
       specification and representation of resources. A specification is
       defined as the result of the content negotiations which are possible,
       giving the handler the information needed to decide which representation
       of the resource to create.

       The handler returns the Representation, which includes the data to be
       returned as a byte array, and a Description, which gives the charsets,
       encodings, etc. that the handler selected for the representation (given
       the Specification). *)

    (* Negotiation *)

    type Specification =
        { Charsets: Negotiation<Charset>
          Encodings: Negotiation<ContentCoding>
          MediaTypes: Negotiation<MediaType>
          Languages: Negotiation<LanguageTag> }

     and Negotiation<'a> =
        | Negotiated of 'a list
        | Free

    (* Specification *)

    type Representation =
        { Data: byte []
          Description: Description }

        static member empty =
            { Data = [||]
              Description =
                { Charset = None
                  Encodings = None
                  MediaType = None
                  Languages = None } }

     and Description =
        { Charset: Charset option
          Encodings: ContentCoding list option
          MediaType: MediaType option
          Languages: LanguageTag list option }

    (* Handling *)

    type Handler =
        Specification -> Freya<Representation>

    (* Negotiation *)

    [<RequireQualifiedAccess>]
    module internal Negotiation =

        (* Negotiation *)

        let negotiated =
            function | Some x -> Negotiated x
                     | _ -> Free

        let negotiable =
            function | Negotiated x when x <> [] -> true
                     | Free -> true
                     | _ -> false

        (* Charset *)

        [<RequireQualifiedAccess>]
        module Charset =

            (* Optics *)

            let accepted_ =
                Request.Headers.acceptCharset_

            let supported_ =
                Properties.Representation.charsetsSupported_

            (* Negotiation *)

            let negotiate supported accepted =
                Option.map (function | AcceptCharset x -> x) accepted
                |> Charset.negotiate supported
                |> negotiated

            (* Configuration *)

            let configure =
                function | TryGet supported_ (Dynamic c) -> Some (negotiate <!> c <*> !. accepted_)
                         | TryGet supported_ (Static c) -> Some (negotiate c <!> !. accepted_)
                         | _ -> None

        (* ContentCoding *)

        [<RequireQualifiedAccess>]
        module ContentCoding =

            (* Optics *)

            let accepted_ =
                Request.Headers.acceptEncoding_

            let supported_ =
                Properties.Representation.contentCodingsSupported_

            (* Negotiation *)

            let negotiate supported accepted =
                Option.map (function | AcceptEncoding x -> x) accepted
                |> ContentCoding.negotiate supported
                |> negotiated

            (* Configuration *)

            let configure =
                function | TryGet supported_ (Dynamic c) -> Some (negotiate <!> c <*> !. accepted_)
                         | TryGet supported_ (Static c) -> Some (negotiate c <!> !. accepted_)
                         | _ -> None

        (* Language *)

        [<RequireQualifiedAccess>]
        module Language =

            (* Optics *)

            let accepted_ =
                Request.Headers.acceptLanguage_

            let supported_ =
                Properties.Representation.languagesSupported_

            (* Negotiation *)

            let negotiate supported acceptLanguage =
                Option.map (function | AcceptLanguage x -> x) acceptLanguage
                |> Language.negotiate supported
                |> negotiated

            (* Configuration *)

            let configure =
                function | TryGet supported_ (Dynamic c) -> Some (negotiate <!> c <*> !. accepted_)
                         | TryGet supported_ (Static c) -> Some (negotiate c <!> !. accepted_)
                         | _ -> None

        (* MediaType *)

        [<RequireQualifiedAccess>]
        module MediaType =

            (* Optics *)

            let accepted_ =
                Request.Headers.accept_

            let supported_ =
                Properties.Representation.mediaTypesSupported_

            (* Negotiation *)

            let negotiate supported accept =
                Option.map (function | Accept x -> x) accept
                |> MediaType.negotiate supported
                |> negotiated

            (* Configuration *)

            let configure =
                function | TryGet supported_ (Dynamic c) -> Some (negotiate <!> c <*> !. accepted_)
                         | TryGet supported_ (Static c) -> Some (negotiate c <!> !. accepted_)
                         | _ -> None

    (* Specification *)

    [<RequireQualifiedAccess>]
    [<CompilationRepresentation (CompilationRepresentationFlags.ModuleSuffix)>]
    module internal Representation =

        let private charsets =
                Negotiation.Charset.configure
             >> function | Some f -> f
                         | _ -> Freya.init Free

        let private specify configuration =
                fun c ->
                    { Charsets = c
                      Encodings = Free
                      MediaTypes = Free
                      Languages = Free }
            <!> charsets configuration

        let private write (r: Representation) =
                Response.body_ %= (fun s -> s.Write (r.Data, 0, r.Data.Length); s)

        let represent configuration handler =
                specify configuration
            >>= handler
            >>= write

(* Operations

   Common operations for standard HTTP responses, setting various header values
   according to the appropriate logic for the response. These are commonly used
   by various terminals within the HTTP Machine.

   Operations are made available at the top level as they are generally useful
   when implementing lower level abstractions but using the Freya stack - thus
   they are made available "as a service"! *)

[<RequireQualifiedAccess>]
module Operations =

    let private date =
        Date.Date >> Some >> (.=) Response.Headers.date_ <| DateTime.UtcNow

    let private phrase =
        Some >> (.=) Response.reasonPhrase_

    let private status =
        Some >> (.=) Response.statusCode_

    (* 2xx *)

    let ok =
            status 200
         *> phrase "OK"
         *> date

    let options =
            status 200
         *> phrase "Options"
         *> date

    let created =
            status 201
         *> phrase "Created"
         *> date

    let accepted =
            status 202
         *> phrase "Accepted"
         *> date

    let noContent =
            status 204
         *> phrase "No Content"
         *> date

    (* 3xx *)

    let multipleChoices =
            status 300
         *> phrase "Multiple Choices"
         *> date

    let movedPermanently =
            status 301
         *> phrase "Moved Permanently"
         *> date

    let found =
            status 302
         *> phrase "Found"
         *> date

    let seeOther =
            status 303
         *> phrase "See Other"
         *> date

    let notModified =
            status 304
         *> phrase "Not Modified"
         *> date

    let temporaryRedirect =
            status 307
         *> phrase "Temporary Redirect"
         *> date

    (* 4xx *)

    let badRequest =
            status 400
         *> phrase "Bad Request"
         *> date

    let unauthorized =
            status 401
         *> phrase "Unauthorized"
         *> date

    let forbidden =
            status 403
         *> phrase "Forbidden"
         *> date

    let notFound =
            status 404
         *> phrase "Not Found"
         *> date

    let methodNotAllowed =
            status 405
         *> phrase "Method Not Allowed"
         *> date

    let notAcceptable =
            status 406
         *> phrase "Not Acceptable"
         *> date

    let conflict =
            status 409
         *> phrase "Conflict"
         *> date

    let gone =
            status 410
         *> phrase "Gone"
         *> date

    let preconditionFailed =
            status 412
         *> phrase "Precondition Failed"
         *> date

    let uriTooLong =
            status 414
         *> phrase "URI Too Long"
         *> date

    let expectationFailed =
            status 417
         *> phrase "Expectation Failed"
         *> date

    (* 5xx *)

    let internalServerError =
            status 500
         *> phrase "Internal Server Error"
         *> date

    let notImplemented =
            status 501
         *> phrase "Not Implemented"
         *> date

    let serviceUnavailable =
            status 503
         *> phrase "Service Unavailable"
         *> date

    let httpVersionNotSupported =
            status 505
         *> phrase "HTTP Version Not Supported"
         *> date

(* Model

   A Hephaestus Model defining the semantics of HTTP execution, defining the
   HTTP request/response process as a series of decisions.

   Unlike other implementations of this model (initially adopted by WebMachine,
   et al.), the Freya implementation does not optimize the graph for lack of
   repetition and reuse, but for straight-line paths, in the knowledge that the
   overall graph will be optimized by the underlying Hephaestus engine. *)

[<RequireQualifiedAccess>]
module Model =

    (* Prelude *)
 
    [<AutoOpen>]
    module internal Prelude =

        (* Key

           Functions for working with Hephaestus Keys, making defining and using
           keys slightly more pleasant. The default empty key is included here
           for consistency at the various levels. *)

        [<RequireQualifiedAccess>]
        module Key =

            let add x =
                Optic.map (Lens.ofIsomorphism Key.key_) ((flip List.append) x)

        (* Decisions *)

        [<RequireQualifiedAccess>]
        module Decision =

            let create (key, name) configurator =
                Specification.Decision.create
                    (Key.add [ name ] key)
                    (configurator >> Decision.map)

            let fromConfiguration (key, name) o def =
                Specification.Decision.create
                    (Key.add [ name ] key)
                    (fun c ->
                        match Optic.get o c with
                        | Some d -> Decision.map d
                        | _ -> Decision.map (Static def))

        [<RequireQualifiedAccess>]
        module Terminal =

            let fromConfiguration (key, name) o operation =
                Specification.Terminal.create
                    (Key.add [ name ] key)
                    (fun c ->
                        match Optic.get o c with
                        | Some h -> operation *> Content.Representation.represent c h
                        | _ -> operation)

    (* Key

       The root key for the "namespace" like functionality implied by the key
       mechanism used in Hephaestus. The machine name is used as the common
       key present in all specifications in this instance. *)

    let private key =
        Key.add [ "http" ] Key.empty

    (* Elements

       Elements (in some cases parameterizable) which may be combined to make
       up specific components which form an HTTP machine. The elements each
       form a specific piece of functionality and are name accordingly. *)

    [<AutoOpen>]
    module Elements =

        (* Assertion

           Decisions asserting the capability of the server to handle the given
           request, based on the defined availability, HTTP protocols, etc.

           Failures of these assertions/checks of server capability will result
           in 5xx error responses, signalling a server error of some type. *)

        [<RequireQualifiedAccess>]
        module Assertion =

            (* Key *)

            let private key p =
                Key.add [ p; "assertion" ] key

            (* Types *)

            type private Assertion =
                { Decisions: Decisions
                  Terminals: Terminals }

                static member decisions_ =
                    (fun x -> x.Decisions), (fun d x -> { x with Decisions = d })

                static member terminals_ =
                    (fun x -> x.Terminals), (fun t x -> { x with Terminals = t })

                static member empty =
                    { Decisions = Decisions.empty
                      Terminals = Terminals.empty }

             and private Decisions =
                { ServiceAvailable: Value<bool> option
                  HttpVersionSupported: Value<bool> option }

                static member serviceAvailable_ =
                    (fun x -> x.ServiceAvailable), (fun s x -> { x with ServiceAvailable = s })

                static member httpVersionSupported_ =
                    (fun x -> x.HttpVersionSupported), (fun h x -> { x with HttpVersionSupported = h })

                static member empty =
                    { ServiceAvailable = None
                      HttpVersionSupported = None }

             and private Terminals =
                { ServiceUnavailable: Content.Handler option
                  HttpVersionNotSupported: Content.Handler option
                  NotImplemented: Content.Handler option }

                static member serviceUnavailable_ =
                    (fun x -> x.ServiceUnavailable), (fun s x -> { x with ServiceUnavailable = s })

                static member httpVersionNotSupported_ =
                    (fun x -> x.HttpVersionNotSupported), (fun h x -> { x with HttpVersionNotSupported = h })

                static member notImplemented_ =
                    (fun x -> x.NotImplemented), (fun n x -> { x with NotImplemented = n })

                static member empty =
                    { ServiceUnavailable = None
                      HttpVersionNotSupported = None
                      NotImplemented = None }

            (* Optics *)

            let private assertion_ =
                    Configuration.element_ Assertion.empty "assertion"

            (* Terminals *)

            [<RequireQualifiedAccess>]
            [<CompilationRepresentation (CompilationRepresentationFlags.ModuleSuffix)>]
            module Terminals =

                let private terminals_ =
                        assertion_
                    >-> Assertion.terminals_

                let serviceUnavailable_ =
                        terminals_
                    >-> Terminals.serviceUnavailable_

                let httpVersionNotSupported_ =
                        terminals_
                    >-> Terminals.httpVersionNotSupported_

                let notImplemented_ =
                        terminals_
                    >-> Terminals.notImplemented_

                let internal serviceUnavailable p =
                    Terminal.fromConfiguration (key p, "service-unavailable-terminal")
                        serviceUnavailable_ Operations.serviceUnavailable

                let internal httpVersionNotSupported p =
                    Terminal.fromConfiguration (key p, "http-version-not-supported")
                        httpVersionNotSupported_ Operations.httpVersionNotSupported

                let internal notImplemented p =
                    Terminal.fromConfiguration (key p, "not-implemented-terminal")
                        notImplemented_ Operations.notImplemented

            (* Decisions *)

            [<RequireQualifiedAccess>]
            [<CompilationRepresentation (CompilationRepresentationFlags.ModuleSuffix)>]
            module Decisions =

                let private decisions_ =
                        assertion_
                    >-> Assertion.decisions_

                let serviceAvailable_ =
                        decisions_
                    >-> Decisions.serviceAvailable_

                let httpVersionSupported_ =
                        decisions_
                    >-> Decisions.httpVersionSupported_

                let rec internal serviceAvailable p s =
                    Decision.fromConfiguration (key p, "service-available-decision")
                        serviceAvailable_ true
                        (Terminals.serviceUnavailable p, httpVersionSupported p s)

                // TODO: Decide on public override?

                and internal httpVersionSupported p s =
                    Decision.fromConfiguration (key p, "http-version-supported-decision")
                        httpVersionSupported_ true
                        (Terminals.httpVersionNotSupported p, methodImplemented p s)

                // TODO: Not Implemented Logic

                and internal methodImplemented p s =
                    Decision.create (key p, "method-implemented-decision")
                        (fun _ -> Static true)
                        (Terminals.notImplemented p, s)

            (* Export *)

            let export =
                Decisions.serviceAvailable

        (* Permission

           Decisions determining the permission of the client to make the
           current request, whether for reasons of authorization or allowance.

           Failures of these checks will result in  401 or 403 responses,
           signalling a client error. *)

        [<RequireQualifiedAccess>]
        module Permission =

            (* Key *)

            let private key p =
                Key.add [ p; "permission" ] key

            (* Types *)

            type private Permission =
                { Decisions: Decisions
                  Terminals: Terminals }

                static member decisions_ =
                    (fun x -> x.Decisions), (fun d x -> { x with Decisions = d })

                static member terminals_ =
                    (fun x -> x.Terminals), (fun t x -> { x with Terminals = t })

                static member empty =
                    { Decisions = Decisions.empty
                      Terminals = Terminals.empty }

             and private Decisions =
                { Authorized: Value<bool> option
                  Allowed: Value<bool> option }

                static member authorized_ =
                    (fun x -> x.Authorized), (fun a x -> { x with Authorized = a })

                static member allowed_ =
                    (fun x -> x.Allowed), (fun a x -> { x with Allowed = a })

                static member empty =
                    { Authorized = None
                      Allowed = None }

             and private Terminals =
                { Unauthorized: Content.Handler option
                  Forbidden: Content.Handler option }

                static member unauthorized_ =
                    (fun x -> x.Unauthorized), (fun u x -> { x with Unauthorized = u })

                static member forbidden_ =
                    (fun x -> x.Forbidden), (fun u x -> { x with Forbidden = u })

                static member empty =
                    { Unauthorized = None
                      Forbidden = None }

            (* Optics *)

            let private permission_ =
                Configuration.element_ Permission.empty "permission"

            (* Terminals *)

            [<RequireQualifiedAccess>]
            [<CompilationRepresentation (CompilationRepresentationFlags.ModuleSuffix)>]
            module Terminals =

                let private terminals_ =
                        permission_
                    >-> Permission.terminals_

                let unauthorized_ =
                        terminals_
                    >-> Terminals.unauthorized_

                let forbidden_ =
                        terminals_
                    >-> Terminals.forbidden_

                let internal unauthorized p =
                    Terminal.fromConfiguration (key p, "unauthorized-terminal")
                        unauthorized_ Operations.unauthorized

                let internal forbidden p =
                    Terminal.fromConfiguration (key p, "forbidden-terminal")
                        forbidden_ Operations.forbidden

            (* Decisions *)

            [<RequireQualifiedAccess>]
            [<CompilationRepresentation (CompilationRepresentationFlags.ModuleSuffix)>]
            module Decisions =

                let private decisions_ =
                        permission_
                    >-> Permission.decisions_

                let authorized_ =
                        decisions_
                    >-> Decisions.authorized_

                let allowed_ =
                        decisions_
                    >-> Decisions.allowed_

                let rec internal authorized p s =
                    Decision.fromConfiguration (key p, "authorized-decision")
                        authorized_ true
                        (Terminals.unauthorized p, allowed p s)

                and internal allowed p s =
                    Decision.fromConfiguration (key p, "allowed-decision")
                        allowed_ true
                        (Terminals.forbidden p, s)

            (* Export *)

            let export =
                Decisions.authorized

        (* Validation

           Decisions determing the basic syntactic and semantic validity of the
           request made by the client, such as whether the method requested is
           allowed, whether the URI is of an appropriate length, etc.

           Failures of these checks will result in 4xx responses of the
           appropriate value, signalling a client error of some type. *)

        [<RequireQualifiedAccess>]
        module Validation =

            (* Key *)

            let private key p =
                Key.add [ p; "validation" ] key

            (* Types *)

            type private Validation =
                { Decisions: Decisions
                  Terminals: Terminals }

                static member decisions_ =
                    (fun x -> x.Decisions), (fun d x -> { x with Decisions = d })

                static member terminals_ =
                    (fun x -> x.Terminals), (fun t x -> { x with Terminals = t })

                static member empty =
                    { Decisions = Decisions.empty
                      Terminals = Terminals.empty }

             and private Decisions =
                { ExpectationMet: Value<bool> option
                  UriTooLong: Value<bool> option
                  BadRequest: Value<bool> option }

                static member expectationMet_ =
                    (fun x -> x.ExpectationMet), (fun e x -> { x with ExpectationMet = e })

                static member uriTooLong_ =
                    (fun x -> x.UriTooLong), (fun u x -> { x with Decisions.UriTooLong = u })

                static member badRequest_ =
                    (fun x -> x.BadRequest), (fun b x -> { x with Decisions.BadRequest = b })

                static member empty =
                    { ExpectationMet = None
                      UriTooLong = None
                      BadRequest = None }

             and private Terminals =
                { ExpectationFailed: Content.Handler option
                  MethodNotAllowed: Content.Handler option
                  UriTooLong: Content.Handler option
                  BadRequest: Content.Handler option }

                static member expectationFailed_ =
                    (fun x -> x.ExpectationFailed), (fun e x -> { x with ExpectationFailed = e })

                static member methodNotAllowed_ =
                    (fun x -> x.MethodNotAllowed), (fun e x -> { x with MethodNotAllowed = e })

                static member uriTooLong_ =
                    (fun x -> x.UriTooLong), (fun u x -> { x with Terminals.UriTooLong = u })

                static member badRequest_ =
                    (fun x -> x.BadRequest), (fun b x -> { x with Terminals.BadRequest = b })

                static member empty =
                    { ExpectationFailed = None
                      MethodNotAllowed = None
                      UriTooLong = None
                      BadRequest = None }

            (* Optics *)

            let private validation_ =
                Configuration.element_ Validation.empty "validation"

            (* Terminals *)

            [<RequireQualifiedAccess>]
            [<CompilationRepresentation (CompilationRepresentationFlags.ModuleSuffix)>]
            module Terminals =

                let private terminals_ =
                        validation_
                    >-> Validation.terminals_

                let expectationFailed_ =
                        terminals_
                    >-> Terminals.expectationFailed_

                let methodNotAllowed_ =
                        terminals_
                    >-> Terminals.methodNotAllowed_

                let uriTooLong_ =
                        terminals_
                    >-> Terminals.uriTooLong_

                let badRequest_ =
                        terminals_
                    >-> Terminals.badRequest_

                let internal expectationFailed p =
                    Terminal.fromConfiguration (key p, "expectation-failed-terminal")
                        expectationFailed_ Operations.expectationFailed

                let internal methodNotAllowed p =
                    Terminal.fromConfiguration (key p, "method-not-allowed-terminal")
                        methodNotAllowed_ Operations.methodNotAllowed

                let internal uriTooLong p =
                    Terminal.fromConfiguration (key p, "uri-too-long-terminal")
                        uriTooLong_ Operations.uriTooLong

                let internal badRequest p =
                    Terminal.fromConfiguration (key p, "bad-request-terminal")
                        badRequest_ Operations.badRequest

            (* Decisions *)

            [<RequireQualifiedAccess>]
            [<CompilationRepresentation (CompilationRepresentationFlags.ModuleSuffix)>]
            module Decisions =

                let private decisions_ =
                        validation_
                    >-> Validation.decisions_

                let private method_ =
                        Request.method_

                let private methodsAllowed_ =
                        Properties.Request.methodsAllowed_

                let expectationMet_ =
                        decisions_
                    >-> Decisions.expectationMet_

                let uriTooLong_ =
                        decisions_
                    >-> Decisions.uriTooLong_

                let badRequest_ =
                        decisions_
                    >-> Decisions.badRequest_

                // TODO: Decide if this is public or not

                let rec internal expectationMet p s =
                    Decision.fromConfiguration (key p, "expectation-met-decision")
                        expectationMet_ true
                        (Terminals.expectationFailed p, methodAllowed p s)

                and internal methodAllowed p s =
                    Decision.create (key p, "method-allowed-decision")
                        (function | TryGet methodsAllowed_ (Dynamic m) -> Dynamic (flip List.contains <!> m <*> !. method_)
                                  | TryGet methodsAllowed_ (Static m) -> Dynamic (flip List.contains m <!> !. method_)
                                  | _ -> Dynamic (flip List.contains Defaults.methodsAllowed <!> !. method_))
                        (Terminals.methodNotAllowed p, uriTooLong p s)

                and internal uriTooLong p s =
                    Decision.fromConfiguration (key p, "uri-too-long-decision")
                        uriTooLong_ false
                        (badRequest p s, Terminals.uriTooLong p)

                and internal badRequest p s =
                    Decision.fromConfiguration (key p, "bad-request-decision")
                        badRequest_ false
                        (s, Terminals.badRequest p)

            (* Export *)

            let export =
                Decisions.expectationMet

        // TODO: This

        (* Negotiation

           Types and functions for handling content negotiation within HTTP. *)

        [<RequireQualifiedAccess>]
        module Negotiation =

            (* Key *)

            let private key p =
                Key.add [ p; "negotiation" ] key

            (* Types *)

            type private Negotiation =
                { Terminals: Terminals }

                static member terminals_ =
                    (fun x -> x.Terminals), (fun t x -> { x with Terminals = t })

                static member empty =
                    { Terminals = Terminals.empty }

             and private Terminals =
                { NotAcceptable: Content.Handler option }

                static member notAcceptable_ =
                    (fun x -> x.NotAcceptable), (fun n x -> { x with NotAcceptable = n })

                static member empty =
                    { NotAcceptable = None }

            (* Optics *)

            let private negotiation_ =
                Configuration.element_ Negotiation.empty "negotiation"

            (* Terminals *)

            [<RequireQualifiedAccess>]
            [<CompilationRepresentation (CompilationRepresentationFlags.ModuleSuffix)>]
            module Terminals =

                let private terminals_ =
                        negotiation_
                    >-> Negotiation.terminals_

                let notAcceptable_ =
                        terminals_
                    >-> Terminals.notAcceptable_

                let internal notAcceptable p =
                    Terminal.fromConfiguration (key p, "not-acceptable-terminal")
                        notAcceptable_ Operations.notAcceptable

            (* Decisions *)

            [<RequireQualifiedAccess>]
            [<CompilationRepresentation (CompilationRepresentationFlags.ModuleSuffix)>]
            module Decisions =

                let rec internal hasAccept p s =
                    Decision.create (key p, "has-accept-decision")
                        (fun _ -> Dynamic (Option.isSome <!> !. Content.Negotiation.MediaType.accepted_))
                        (hasAcceptLanguage p s, acceptMatches p s)

                and internal acceptMatches p s =
                    Decision.create (key p, "accept-matches-decision")
                        (Content.Negotiation.MediaType.configure 
                         >> function | Some f -> Dynamic (Content.Negotiation.negotiable <!> f)
                                     | _ -> Static true)
                        (Terminals.notAcceptable p, hasAcceptLanguage p s)

                and internal hasAcceptLanguage p s =
                    Decision.create (key p, "has-accept-language-decision")
                        (fun _ -> Dynamic (Option.isSome <!> !. Content.Negotiation.Language.accepted_))
                        (hasAcceptCharset p s, acceptLanguageMatches p s)

                and internal acceptLanguageMatches p s =
                    Decision.create (key p, "accept-language-matches-decision")
                        (Content.Negotiation.Language.configure
                         >> function | Some f -> Dynamic (Content.Negotiation.negotiable <!> f)
                                     | _ -> Static true)
                        (Terminals.notAcceptable p, hasAcceptCharset p s)

                and internal hasAcceptCharset p s =
                    Decision.create (key p, "has-accept-charset-decision")
                        (fun _ -> Dynamic (Option.isSome <!> !. Content.Negotiation.Charset.accepted_))
                        (hasAcceptEncoding p s, acceptCharsetMatches p s)

                and internal acceptCharsetMatches p s =
                    Decision.create (key p, "accept-charset-matches-decision")
                        (Content.Negotiation.Charset.configure
                         >> function | Some f -> Dynamic (Content.Negotiation.negotiable <!> f)
                                     | _ -> Static true)
                        (Terminals.notAcceptable p, hasAcceptEncoding p s)

                and internal hasAcceptEncoding p s =
                    Decision.create (key p, "has-accept-encoding-decision")
                        (fun _ -> Dynamic (Option.isSome <!> !. Content.Negotiation.ContentCoding.accepted_))
                        (s, acceptEncodingMatches p s)

                and internal acceptEncodingMatches p s =
                    Decision.create (key p, "accept-encoding-matches-decision")
                        (Content.Negotiation.ContentCoding.configure
                         >> function | Some f -> Dynamic (Content.Negotiation.negotiable <!> f)
                                     | _ -> Static true)
                        (Terminals.notAcceptable p, s)

            (* Export *)

            let internal export =
                Decisions.hasAccept

        (* Method

           Decision determining whether the method matches any of a supplied
           list of methods given as part of parameterization of this element.

           The element does not result in a response, only in control flow of
           the machine, and as such must be provided with both left and right
           cases (no terminals are implied).

           NOTE: This decision has a significant optimization, in that it will
           become a Static value of false if the method to be matched is not
           an allowed method for this resource. This aids significantly in
           graph optimization, but does imply that a method validation check
           should be put of the workflow (i.e. that it should correctly be
           preceded by a Validation element). *)

        [<RequireQualifiedAccess>]
        module Method =

            [<RequireQualifiedAccess>]
            module private Seq =

                let disjoint l1 l2 =
                    Set.isEmpty (Set.intersect (set l1) (set l2))

            (* Key *)

            let private key p =
                Key.add [ p; "method" ] key

            (* Decisions *)

            [<RequireQualifiedAccess>]
            [<CompilationRepresentation (CompilationRepresentationFlags.ModuleSuffix)>]
            module Decisions =

                let private method_ =
                        Request.method_

                let private methodsAllowed_ =
                        Properties.Request.methodsAllowed_

                let internal methodMatches p ms =
                    Decision.create (key p, "method-matches-decision")
                        (function | TryGet methodsAllowed_ (Static ms') when Seq.disjoint ms ms' -> Static false
                                  | TryGet methodsAllowed_ _ -> Dynamic (flip List.contains ms <!> !. method_)
                                  | _ when Seq.disjoint ms Defaults.methodsAllowed -> Static false
                                  | _ -> Dynamic (flip List.contains ms <!> !. method_))

            (* Export *)

            let export =
                Decisions.methodMatches

        (* Existence

           Decision determining whether or not the relevant resource exists at
           this point (it may have existed previously, but this is about the
           existence of the resource currently).

           The element does not result in a response, only in control flow of
           the machine, and as such must be provided with both left and right
           cases (no terminals are implied). *)

        [<RequireQualifiedAccess>]
        module Existence =

            let private key p =
                Key.add [ p; "existence" ] key

            (* Types *)

            type private Existence =
                { Decisions: Decisions }

                static member decisions_ =
                    (fun x -> x.Decisions), (fun d x -> { x with Decisions = d })

                static member empty =
                    { Decisions = Decisions.empty }

             and private Decisions =
                { Exists: Value<bool> option }

                static member exists_ =
                    (fun x -> x.Exists), (fun e x -> { x with Exists = e })

                static member empty =
                    { Exists = None }

            (* Optics *)

            let private existence_ =
                Configuration.element_ Existence.empty "existence"

            (* Decisions *)

            [<RequireQualifiedAccess>]
            [<CompilationRepresentation (CompilationRepresentationFlags.ModuleSuffix)>]
            module Decisions =

                let private decisions_ =
                        existence_
                    >-> Existence.decisions_

                let exists_ =
                        decisions_
                    >-> Decisions.exists_

                let internal exists p =
                    Decision.fromConfiguration (key p, "exists-decision")
                        exists_ true

            (* Export *)

            let export =
                Decisions.exists

        (* Preconditions

           Decisions representing the negotiation of optional preconditions
           declared by the client, which should preclude further processing of
           the request if not met. Preconditions are divided in to common
           (applying to all requests) and safe/unsafe, where the unsafe set
           are applicable to methods which would change state.

           Preconditions failing results in a 412 response. *)

        [<RequireQualifiedAccess>]
        module Preconditions =

            (* Key *)

            let private key p =
                Key.add [ p; "preconditions" ] key

            (* Types *)

            type private Preconditions =
                { Terminals: Terminals }

                static member terminals_ =
                    (fun x -> x.Terminals), (fun t x -> { x with Terminals = t })

                static member empty =
                    { Terminals = Terminals.empty }

             and Terminals =
                { PreconditionFailed: Content.Handler option }

                static member preconditionFailed_ =
                    (fun x -> x.PreconditionFailed), (fun p x -> { x with PreconditionFailed = p })

                static member empty =
                    { PreconditionFailed = None }

            (* Optics *)

            let private preconditions_ =
                Configuration.element_ Preconditions.empty "preconditions"

            let private eTags_ =
                  Properties.Resource.eTags_

            let private lastModified_ =
                  Properties.Resource.lastModified_

            (* Shared *)

            [<RequireQualifiedAccess>]
            module Shared =

                (* Terminals *)

                [<RequireQualifiedAccess>]
                module Terminals =

                    let private terminals_ =
                            preconditions_
                        >-> Preconditions.terminals_

                    // TODO: Make sure this has syntax

                    let preconditionFailed_ =
                            terminals_
                        >-> Terminals.preconditionFailed_

                    let internal preconditionFailed p =
                        Terminal.fromConfiguration (key p, "precondition-failed-terminal")
                            preconditionFailed_ Operations.preconditionFailed

            (* Common *)

            [<RequireQualifiedAccess>]
            module Common =

                (* Key *)

                let private key p =
                    Key.add [ "common" ] (key p)

                (* Decisions *)

                [<RequireQualifiedAccess>]
                [<CompilationRepresentation (CompilationRepresentationFlags.ModuleSuffix)>]
                module Decisions =

                    let private ifMatch_ =
                            Request.Headers.ifMatch_

                    let private ifUnmodifiedSince_ =
                            Request.Headers.ifUnmodifiedSince_

                    let rec internal hasIfMatch p s =
                        Decision.create (key p, "has-if-match-decision")
                            (fun _ -> Dynamic (Option.isSome <!> !. ifMatch_))
                            (hasIfUnmodifiedSince p s, ifMatchMatches p s)

                    // TODO: Logic

                    and internal ifMatchMatches p s =
                        Decision.create (key p, "if-match-matches-decision")
                            (function | TryGet eTags_ (Dynamic _) -> Static true
                                      | _ -> Static true)
                            (Shared.Terminals.preconditionFailed p, s)

                    and internal hasIfUnmodifiedSince p s =
                        Decision.create (key p, "has-if-unmodified-since-decision")
                            (fun _ -> Dynamic (Option.isSome <!> !. ifUnmodifiedSince_))
                            (s, ifUnmodifiedSinceMatches p s)

                    // TODO: Logic

                    and internal ifUnmodifiedSinceMatches p s =
                        Decision.create (key p, "if-unmodified-since-matches-decision")
                            (function | TryGet lastModified_ (Dynamic _) -> Static true
                                      | _ -> Static true)
                            (Shared.Terminals.preconditionFailed p, s)

                (* Export *)

                let export =
                    Decisions.hasIfMatch

            (* Safe *)

            [<RequireQualifiedAccess>]
            module Safe =

                (* Key *)

                let private key p =
                    Key.add [ "safe" ] (key p)

                (* Types *)

                type private Safe =
                    { Terminals: Terminals }

                    static member terminals_ =
                        (fun x -> x.Terminals), (fun t x -> { x with Safe.Terminals = t })

                    static member empty =
                        { Terminals = Terminals.empty }

                 and Terminals =
                    { NotModified: Content.Handler option }

                    static member notModified_ =
                        (fun x -> x.NotModified), (fun n x -> { x with NotModified = n })

                    static member empty =
                        { NotModified = None }

                (* Optics *)

                let private safe_ =
                    Configuration.element_ Safe.empty "preconditions.safe"

                (* Terminals *)

                [<RequireQualifiedAccess>]
                [<CompilationRepresentation (CompilationRepresentationFlags.ModuleSuffix)>]
                module Terminals =

                    let private terminals_ =
                            safe_
                        >-> Safe.terminals_

                    let notModified_ =
                            terminals_
                        >-> Terminals.notModified_

                    let internal notModified p =
                        Terminal.fromConfiguration (key p, "not-modified-terminal")
                            notModified_ Operations.notModified

                (* Decisions *)

                [<RequireQualifiedAccess>]
                [<CompilationRepresentation (CompilationRepresentationFlags.ModuleSuffix)>]
                module Decisions =

                    let private ifNoneMatch_ =
                            Request.Headers.ifNoneMatch_

                    let private ifModifiedSince_ =
                            Request.Headers.ifModifiedSince_

                    let rec internal hasIfNoneMatch p s =
                        Decision.create (key p, "has-if-none-match-decision")
                            (fun _ -> Dynamic (Option.isSome <!> !. ifNoneMatch_))
                            (hasIfModifiedSince p s, ifNoneMatchMatches p s)

                    // TODO: Logic

                    and internal ifNoneMatchMatches p s =
                        Decision.create (key p, "if-none-match-matches-decision")
                            (function | _ -> Static true)
                            (Terminals.notModified p, s)

                    and internal hasIfModifiedSince p s =
                        Decision.create (key p, "has-if-modified-since-decision")
                            (fun _ -> Dynamic (Option.isSome <!> !. ifModifiedSince_))
                            (s, ifModifiedSinceMatches p s)

                    // TODO: Logic

                    and internal ifModifiedSinceMatches p s =
                        Decision.create (key p, "if-modified-since-matches-decision")
                            (function | _ -> Static true)
                            (Terminals.notModified p, s)

                (* Export *)

                let export =
                    Decisions.hasIfNoneMatch

            (* Unsafe *)

            [<RequireQualifiedAccess>]
            module Unsafe =

                (* Key *)

                let private key p =
                    Key.add [ "unsafe" ] (key p)

                (* Decisions *)

                [<RequireQualifiedAccess>]
                [<CompilationRepresentation (CompilationRepresentationFlags.ModuleSuffix)>]
                module Decisions =

                    let private ifNoneMatch_ =
                            Request.Headers.ifNoneMatch_

                    let private eTags_ =
                            Properties.Resource.eTags_

                    // TODO: Logic

                    let rec internal hasIfNoneMatch p s =
                        Decision.create (key p, "has-if-none-match-decision")
                            (function | _ -> Static true)
                            (s, ifNoneMatchMatches p s)

                    // TODO: Logic

                    and internal ifNoneMatchMatches p s =
                        Decision.create (key p, "if-none-match-matches-decision")
                            (function | _ -> Static true)
                            (Shared.Terminals.preconditionFailed p, s)

                (* Export *)

                let export =
                    Decisions.hasIfNoneMatch

        (* Conflict

           Decision determining whether the requested operation would cause a
           conflict given the current state of the resource.

           Where a conflict would be caused, a 409 response is returned,
           signalling a client error. *)

        [<RequireQualifiedAccess>]
        module Conflict =

            (* Key *)

            let private key p =
                Key.add [ p; "conflict" ] key

            (* Types *)

            type private Conflict =
                { Decisions: Decisions
                  Terminals: Terminals }

                static member decisions_ =
                    (fun x -> x.Decisions), (fun d x -> { x with Decisions = d })

                static member terminals_ =
                    (fun x -> x.Terminals), (fun t x -> { x with Terminals = t })

                static member empty =
                    { Decisions = Decisions.empty
                      Terminals = Terminals.empty }

             and private Decisions =
                { Conflict: Value<bool> option }

                static member conflict_ =
                    (fun x -> x.Conflict), (fun e x -> { x with Decisions.Conflict = e })

                static member empty =
                    { Conflict = None }

             and private Terminals =
                { Conflict: Content.Handler option }

                static member conflict_ =
                    (fun x -> x.Conflict), (fun e x -> { x with Terminals.Conflict = e })

                static member empty =
                    { Conflict = None }

            (* Optics *)

            let private conflict_ =
                Configuration.element_ Conflict.empty "conflict"

            (* Terminals *)

            [<RequireQualifiedAccess>]
            [<CompilationRepresentation (CompilationRepresentationFlags.ModuleSuffix)>]
            module Terminals =

                let private terminals_ =
                        conflict_
                    >-> Conflict.terminals_

                let conflict_ =
                        terminals_
                    >-> Terminals.conflict_

                let internal conflict p =
                    Terminal.fromConfiguration (key p, "conflict-terminal")
                        conflict_ Operations.conflict

            (* Decisions *)

            [<RequireQualifiedAccess>]
            [<CompilationRepresentation (CompilationRepresentationFlags.ModuleSuffix)>]
            module Decisions =

                let private decisions_ =
                        conflict_
                    >-> Conflict.decisions_

                let conflict_ =
                        decisions_
                    >-> Decisions.conflict_

                let internal conflict p s =
                    Decision.fromConfiguration (key p, "conflict-decision")
                        conflict_ true
                        (s, Terminals.conflict p)

            (* Export *)

            let export =
                Decisions.conflict

        (* Operation

           Decisions representing the processing of a specific operation (one
           of the non-idempotent methods supported by the HTTP model). The
           decision for the operation is a Freya<bool> and thus will always
           remain dynamic throughout optimization (if present). *)

        [<RequireQualifiedAccess>]
        module Operation =

            (* Key *)

            let private key p =
                Key.add [ p; "operation" ] key

            (* Types *)

            type private Operation =
                { Operations: Operations
                  Decisions: Decisions
                  Terminals: Terminals }

                static member operations_ =
                    (fun x -> x.Operations), (fun o x -> { x with Operation.Operations = o })

                static member decisions_ =
                    (fun x -> x.Decisions), (fun d x -> { x with Decisions = d })

                static member terminals_ =
                    (fun x -> x.Terminals), (fun t x -> { x with Terminals = t })

                static member empty =
                    { Operations = Operations.empty
                      Decisions = Decisions.empty
                      Terminals = Terminals.empty }

             and private Operations =
                { Operations: Map<Method,Freya<bool>> }

                static member operations_ =
                    (fun x -> x.Operations), (fun o x -> { x with Operations.Operations = o })

                static member empty =
                    { Operations = Map.empty }

             and private Decisions =
                { Completed: Value<bool> option }

                static member completed_ =
                    (fun x -> x.Completed), (fun c x -> { x with Completed = c })

                static member empty =
                    { Completed = None }

             and private Terminals =
                { InternalServerError: Content.Handler option
                  Accepted: Content.Handler option }

                static member internalServerError_ =
                    (fun x -> x.InternalServerError), (fun i x -> { x with InternalServerError = i })

                static member accepted_ =
                    (fun x -> x.Accepted), (fun a x -> { x with Accepted = a })

                static member empty =
                    { InternalServerError = None
                      Accepted = None }

            (* Optics *)

            let private operation_ =
                Configuration.element_ Operation.empty "operation"

            (* Terminals *)

            [<RequireQualifiedAccess>]
            [<CompilationRepresentation (CompilationRepresentationFlags.ModuleSuffix)>]
            module Terminals =

                let private terminals_ =
                        operation_
                    >-> Operation.terminals_

                let internalServerError_ =
                        terminals_
                    >-> Terminals.internalServerError_

                let accepted_ =
                        terminals_
                    >-> Terminals.accepted_

                let internal internalServerError p =
                    Terminal.fromConfiguration (key p, "internal-server-error-terminal")
                        internalServerError_ Operations.internalServerError

                let internal accepted p =
                    Terminal.fromConfiguration (key p, "accepted-terminal")
                        accepted_ Operations.accepted

            (* Decisions *)

            [<RequireQualifiedAccess>]
            [<CompilationRepresentation (CompilationRepresentationFlags.ModuleSuffix)>]
            module Decisions =

                let private operations_ =
                        operation_
                    >-> Operation.operations_

                let operationMethod_ m =
                        operations_
                    >-> Operations.operations_
                    >-> Map.value_ m

                let private decisions_ =
                        operation_
                    >-> Operation.decisions_

                let completed_ =
                        decisions_
                    >-> Decisions.completed_

                let rec internal operation p m s =
                    Decision.create (key p, "operation-decision")
                        (function | Get (operationMethod_ m) (Some f) -> Dynamic (f)
                                  | _ -> Static true)
                        (Terminals.internalServerError p, completed p s)

                and internal completed p s =
                    Decision.fromConfiguration (key p, "completed-decision")
                        completed_ true
                        (s, Terminals.accepted p)

            (* Export *)

            let export =
                Decisions.operation

        (* Responses *)

        [<RequireQualifiedAccess>]
        module Responses =

            (* Key *)

            let private key p =
                Key.add [ p; "responses" ] key

            (* Common *)

            [<RequireQualifiedAccess>]
            module Common =

                (* Key *)

                let private key p =
                    Key.add [ "common" ] (key p)

                (* Types *)

                type private Common =
                    { Decisions: Decisions
                      Terminals: Terminals }

                    static member decisions_ =
                        (fun x -> x.Decisions), (fun d x -> { x with Decisions = d })

                    static member terminals_ =
                        (fun x -> x.Terminals), (fun t x -> { x with Terminals = t })

                    static member empty =
                        { Decisions = Decisions.empty
                          Terminals = Terminals.empty }

                 and private Decisions =
                    { NoContent: Value<bool> option }

                    static member noContent_ =
                        (fun x -> x.NoContent), (fun n x -> { x with Decisions.NoContent = n })

                    static member empty =
                        { NoContent = None }

                 and private Terminals =
                    { NoContent: Content.Handler option
                      Ok: Content.Handler option }

                    static member noContent_ =
                        (fun x -> x.NoContent), (fun n x -> { x with Terminals.NoContent = n })

                    static member ok_ =
                        (fun x -> x.Ok), (fun o x -> { x with Ok = o })

                    static member empty =
                        { NoContent = None
                          Ok = None }

                (* Optics *)

                let private common_ =
                        Configuration.element_ Common.empty "responses.common"

                (* Terminals *)

                [<RequireQualifiedAccess>]
                [<CompilationRepresentation (CompilationRepresentationFlags.ModuleSuffix)>]
                module Terminals =

                    let private terminals_ =
                            common_
                        >-> Common.terminals_

                    let noContent_ =
                            terminals_
                        >-> Terminals.noContent_

                    let ok_ =
                            terminals_
                        >-> Terminals.ok_

                    let internal noContent p =
                        Terminal.fromConfiguration (key p, "no-content-terminal")
                            noContent_ Operations.noContent

                    let internal ok p =
                        Terminal.fromConfiguration (key p, "ok-terminal")
                            ok_ Operations.ok

                (* Decisions *)

                [<RequireQualifiedAccess>]
                [<CompilationRepresentation (CompilationRepresentationFlags.ModuleSuffix)>]
                module Decisions =

                    let private decisions_ =
                            common_
                        >-> Common.decisions_

                    let noContent_ =
                            decisions_
                        >-> Decisions.noContent_

                    let internal noContent p =
                        Decision.fromConfiguration (key p, "no-content-decision")
                            noContent_ false
                            (Terminals.ok p, Terminals.noContent p)

                (* Export *)

                let export =
                    Decisions.noContent

            (* Created *)

            [<RequireQualifiedAccess>]
            module Created =

                (* Key *)

                let private key p =
                    Key.add [ "created" ] (key p)

                (* Types *)

                type private Created =
                    { Decisions: Decisions
                      Terminals: Terminals }

                    static member decisions_ =
                        (fun x -> x.Decisions), (fun d x -> { x with Decisions = d })

                    static member terminals_ =
                        (fun x -> x.Terminals), (fun t x -> { x with Terminals = t })

                    static member empty =
                        { Decisions = Decisions.empty
                          Terminals = Terminals.empty }

                 and private Decisions =
                    { Created: Value<bool> option }

                    static member created_ =
                        (fun x -> x.Created), (fun e x -> { x with Decisions.Created = e })

                    static member empty =
                        { Created = None }

                 and private Terminals =
                    { Created: Content.Handler option }

                    static member created_ =
                        (fun x -> x.Created), (fun e x -> { x with Terminals.Created = e })

                    static member empty =
                        { Created = None }

                (* Optics *)

                let private created_ =
                    Configuration.element_ Created.empty "responses.created"

                (* Terminals *)

                [<RequireQualifiedAccess>]
                [<CompilationRepresentation (CompilationRepresentationFlags.ModuleSuffix)>]
                module Terminals =

                    let private terminals_ =
                            created_
                        >-> Created.terminals_

                    let created_ =
                            terminals_
                        >-> Terminals.created_

                    let internal created p =
                        Terminal.fromConfiguration (key p, "created-terminal")
                            created_ Operations.created

                (* Decisions *)

                [<RequireQualifiedAccess>]
                [<CompilationRepresentation (CompilationRepresentationFlags.ModuleSuffix)>]
                module Decisions =

                    let private decisions_ =
                            created_
                        >-> Created.decisions_

                    let created_ =
                            decisions_
                        >-> Decisions.created_

                    let internal created p s =
                        Decision.fromConfiguration (key p, "created-decision")
                            created_ false
                            (s, Terminals.created p)

                (* Export *)

                let export =
                    Decisions.created

            (* Missing *)

            [<RequireQualifiedAccess>]
            module Missing =

                (* Key *)

                let private key p =
                    Key.add [ "missing" ] (key p)

                (* Types *)

                type private Missing =
                    { Terminals: Terminals }

                    static member terminals_ =
                        (fun x -> x.Terminals), (fun t x -> { x with Terminals = t })

                    static member empty =
                        { Terminals = Terminals.empty }

                 and private Terminals =
                    { NotFound: Content.Handler option }

                    static member notFound_ =
                        (fun x -> x.NotFound), (fun n x -> { x with NotFound = n })

                    static member empty =
                        { NotFound = None }

                (* Optics *)

                let private missing_ =
                    Configuration.element_ Missing.empty "responses.missing"

                (* Terminals *)

                [<RequireQualifiedAccess>]
                [<CompilationRepresentation (CompilationRepresentationFlags.ModuleSuffix)>]
                module Terminals =

                    let private terminals_ =
                            missing_
                        >-> Missing.terminals_

                    let notFound_ =
                            terminals_
                        >-> Terminals.notFound_

                    let internal notFound p =
                        Terminal.fromConfiguration (key p, "not-found-terminal")
                            notFound_ Operations.notFound

                (* Export *)

                let export =
                    Terminals.notFound

            (* Moved *)

            [<RequireQualifiedAccess>]
            module Moved =

                (* Key *)

                let private key p =
                    Key.add [ "other" ] (key p)

                (* Types *)

                type private Moved =
                    { Decisions: Decisions
                      Terminals: Terminals }

                    static member decisions_ =
                        (fun x -> x.Decisions), (fun d x -> { x with Decisions = d })

                    static member terminals_ =
                        (fun x -> x.Terminals), (fun t x -> { x with Terminals = t })

                    static member empty =
                        { Decisions = Decisions.empty
                          Terminals = Terminals.empty }

                 and private Decisions =
                    { Gone: Value<bool> option
                      MovedTemporarily: Value<bool> option
                      MovedPermanently: Value<bool> option }

                    static member gone_ =
                        (fun x -> x.Gone), (fun g x -> { x with Decisions.Gone = g })

                    static member movedTemporarily_ =
                        (fun x -> x.MovedTemporarily), (fun m x -> { x with MovedTemporarily = m })

                    static member movedPermanently_ =
                        (fun x -> x.MovedPermanently), (fun m x -> { x with Decisions.MovedPermanently = m })

                    static member empty =
                        { Gone = None
                          MovedTemporarily = None
                          MovedPermanently = None }

                 and private Terminals =
                    { Gone: Content.Handler option
                      TemporaryRedirect: Content.Handler option
                      MovedPermanently: Content.Handler option }

                    static member gone_ =
                        (fun x -> x.Gone), (fun g x -> { x with Terminals.Gone = g })

                    static member temporaryRedirect_ =
                        (fun x -> x.TemporaryRedirect), (fun t x -> { x with TemporaryRedirect = t })

                    static member movedPermanently_ =
                        (fun x -> x.MovedPermanently), (fun m x -> { x with Terminals.MovedPermanently = m })

                    static member empty =
                        { Gone = None
                          TemporaryRedirect = None
                          MovedPermanently = None }

                (* Optics *)

                let private moved_ =
                        Configuration.element_ Moved.empty "responses.moved"

                (* Terminals *)

                [<RequireQualifiedAccess>]
                [<CompilationRepresentation (CompilationRepresentationFlags.ModuleSuffix)>]
                module Terminals =

                    let private terminals_ =
                            moved_
                        >-> Moved.terminals_

                    let gone_ =
                            terminals_
                        >-> Terminals.gone_

                    let temporaryRedirect_ =
                            terminals_
                        >-> Terminals.temporaryRedirect_

                    let movedPermanently_ =
                            terminals_
                        >-> Terminals.movedPermanently_

                    let internal gone p =
                        Terminal.fromConfiguration (key p, "gone-terminal")
                            gone_ Operations.gone

                    let internal temporaryRedirect p =
                        Terminal.fromConfiguration (key p, "temporary-redirect-terminal")
                            temporaryRedirect_ Operations.temporaryRedirect

                    let internal movedPermanently p =
                        Terminal.fromConfiguration (key p, "moved-permanently-terminal")
                            movedPermanently_ Operations.movedPermanently

                (* Decisions *)

                [<RequireQualifiedAccess>]
                [<CompilationRepresentation (CompilationRepresentationFlags.ModuleSuffix)>]
                module Decisions =

                    let private decisions_ =
                            moved_
                        >-> Moved.decisions_

                    let gone_ =
                            decisions_
                        >-> Decisions.gone_

                    let movedTemporarily_ =
                            decisions_
                        >-> Decisions.movedTemporarily_

                    let movedPermanently_ =
                            decisions_
                        >-> Decisions.movedPermanently_

                    let rec internal gone p s =
                        Decision.fromConfiguration (key p, "see-other-decision")
                            gone_ false
                            (movedTemporarily p s, Terminals.gone p)

                    and internal movedTemporarily p s =
                        Decision.fromConfiguration (key p, "found-decision")
                            movedTemporarily_ false
                            (movedPermanently p s, Terminals.temporaryRedirect p)

                    and internal movedPermanently p s =
                        Decision.fromConfiguration (key p, "see-other-decision")
                            movedPermanently_ false
                            (s, Terminals.movedPermanently p)

                (* Export *)

                let export =
                    Decisions.gone

            (* Options *)

            [<RequireQualifiedAccess>]
            module Options =

                (* Key *)

                let private key p =
                    Key.add [ "options" ] (key p)

                 (* Types *)

                type private Options =
                    { Terminals: Terminals }

                    static member terminals_ =
                        (fun x -> x.Terminals), (fun t x -> { x with Terminals = t })

                    static member empty =
                        { Terminals = Terminals.empty }

                 and private Terminals =
                    { Options: Content.Handler option }

                    static member options_ =
                        (fun x -> x.Options), (fun n x -> { x with Options = n })

                    static member empty =
                        { Options = None }

                (* Optics *)

                let private options_ =
                    Configuration.element_ Options.empty "responses.options"

                (* Terminals *)

                [<RequireQualifiedAccess>]
                [<CompilationRepresentation (CompilationRepresentationFlags.ModuleSuffix)>]
                module Terminals =

                    let private terminals_ =
                            options_
                        >-> Options.terminals_

                    let options_ =
                            terminals_
                        >-> Terminals.options_

                    let internal options p =
                        Terminal.fromConfiguration (key p, "options-terminal")
                            options_ Operations.options

                (* Export *)

                let export =
                    Terminals.options

            (* Other *)

            [<RequireQualifiedAccess>]
            module Other =

                (* Key *)

                let private key p =
                    Key.add [ "other" ] (key p)

                (* Types *)

                type private Other =
                    { Decisions: Decisions
                      Terminals: Terminals }

                    static member decisions_ =
                        (fun x -> x.Decisions), (fun d x -> { x with Decisions = d })

                    static member terminals_ =
                        (fun x -> x.Terminals), (fun t x -> { x with Terminals = t })

                    static member empty =
                        { Decisions = Decisions.empty
                          Terminals = Terminals.empty }

                 and private Decisions =
                    { SeeOther: Value<bool> option
                      Found: Value<bool> option
                      MultipleChoices: Value<bool> option }

                    static member seeOther_ =
                        (fun x -> x.SeeOther), (fun s x -> { x with Decisions.SeeOther = s })

                    static member found_ =
                        (fun x -> x.Found), (fun f x -> { x with Decisions.Found = f })

                    static member multipleChoices_ =
                        (fun x -> x.MultipleChoices), (fun m x -> { x with Decisions.MultipleChoices = m })

                    static member empty =
                        { SeeOther = None
                          Found = None
                          MultipleChoices = None }

                 and private Terminals =
                    { SeeOther: Content.Handler option
                      Found: Content.Handler option
                      MultipleChoices: Content.Handler option }

                    static member seeOther_ =
                        (fun x -> x.SeeOther), (fun s x -> { x with Terminals.SeeOther = s })

                    static member found_ =
                        (fun x -> x.Found), (fun f x -> { x with Terminals.Found = f })

                    static member multipleChoices_ =
                        (fun x -> x.MultipleChoices), (fun m x -> { x with Terminals.MultipleChoices = m })

                    static member empty =
                        { SeeOther = None
                          Found = None
                          MultipleChoices = None }

                (* Optics *)

                let private other_ =
                        Configuration.element_ Other.empty "responses.other"

                (* Terminals *)

                [<RequireQualifiedAccess>]
                [<CompilationRepresentation (CompilationRepresentationFlags.ModuleSuffix)>]
                module Terminals =

                    let private terminals_ =
                            other_
                        >-> Other.terminals_

                    let seeOther_ =
                            terminals_
                        >-> Terminals.seeOther_

                    let found_ =
                            terminals_
                        >-> Terminals.found_

                    let multipleChoices_ =
                            terminals_
                        >-> Terminals.multipleChoices_

                    let internal seeOther p =
                        Terminal.fromConfiguration (key p, "see-other-terminal")
                            seeOther_ Operations.seeOther

                    let internal found p =
                        Terminal.fromConfiguration (key p, "found-terminal")
                            found_ Operations.found

                    let internal multipleChoices p =
                        Terminal.fromConfiguration (key p, "multiple-choices-terminal")
                            multipleChoices_ Operations.multipleChoices

                (* Decisions *)

                [<RequireQualifiedAccess>]
                [<CompilationRepresentation (CompilationRepresentationFlags.ModuleSuffix)>]
                module Decisions =

                    let private decisions_ =
                            other_
                        >-> Other.decisions_

                    let seeOther_ =
                            decisions_
                        >-> Decisions.seeOther_

                    let found_ =
                            decisions_
                        >-> Decisions.found_

                    let multipleChoices_ =
                            decisions_
                        >-> Decisions.multipleChoices_

                    let rec internal seeOther p s =
                        Decision.fromConfiguration (key p, "see-other-decision")
                            seeOther_ false
                            (found p s, Terminals.seeOther p)

                    and internal found p s =
                        Decision.fromConfiguration (key p, "found-decision")
                            found_ false
                            (multipleChoices p s, Terminals.found p)

                    and internal multipleChoices p s =
                        Decision.fromConfiguration (key p, "see-other-decision")
                            multipleChoices_ false
                            (s, Terminals.multipleChoices p)

                (* Export *)

                let export =
                    Decisions.seeOther

    (* Components

       The components of an HTTP machine model, formed by composing and in some
       cases parameterizing elements in specific orders to give a useful HTTP
       processing cycle.

       In this HTTP machine the components are defined as a single common core
       component, and additional components for each of the methods, being
       spliced in to the core component at appropriate points. *)

    [<AutoOpen>]
    module Components =

        (* Core *)

        [<RequireQualifiedAccess>]
        module Core =

            [<Literal>]
            let private Core =
                "core"

            (* Terminals *)

            // TODO: Fix this up!

            let private endpointTerminal =
                Terminal.fromConfiguration (key, "end-terminal")
                    ((fun _ -> None), (fun _ c -> c)) Operations.ok

            (* Decisions *)

            let private endpointDecision =
                Decision.create (key, "end-decision")
                    (fun _ -> Static true)
                    (Specification.Terminal.empty, endpointTerminal)

            (* Export *)

            let private core =
                Assertion.export Core (
                    Permission.export Core (
                        Validation.export Core (
                            Negotiation.export Core endpointDecision)))

            let export =
                { Metadata =
                    { Name = "http.core"
                      Description = None }
                  Requirements =
                    { Required = Set.empty
                      Preconditions = List.empty }
                  Operations =
                    [ Prepend (fun _ -> core) ] }

        (* Get or Head *)

        [<RequireQualifiedAccess>]
        module GetOrHead =

            [<Literal>]
            let private GetOrHead =
                "get-or-head"

            (* Export *)

            let private getOrHead s =
                Method.export GetOrHead [ GET; HEAD ] (
                    s, Existence.export GetOrHead (
                        Responses.Moved.export GetOrHead (
                            Responses.Missing.export GetOrHead),
                        Preconditions.Common.export GetOrHead (
                            Preconditions.Safe.export GetOrHead (
                                Responses.Other.export GetOrHead (
                                    Responses.Common.export GetOrHead)))))

            let export =
                { Metadata =
                    { Name = "http.get"
                      Description = None }
                  Requirements =
                    { Required = set [ "http.core" ]
                      Preconditions = List.empty }
                  Operations =
                    [ Splice (Key [ "http"; "end-decision" ], Right, getOrHead) ] }

        (* Options *)

        [<RequireQualifiedAccess>]
        module Options =

            [<Literal>]
            let private Options =
                "options"

            (* Export *)

            let private options s =
                Method.export Options [ OPTIONS ] (
                    s, Responses.Options.export Options)

            let export =
                { Metadata =
                    { Name = "http.options"
                      Description = None }
                  Requirements =
                    { Required = set [ "http.core" ]
                      Preconditions = List.empty }
                  Operations =
                    [ Splice (Key [ "http"; "core"; "validation"; "bad-request-decision" ], Left, options) ] }

        (* Post *)

        [<RequireQualifiedAccess>]
        module Post =

            [<Literal>]
            let private Post =
                "post"

            (* Export *)

            let private post s =
                Method.export Post [ POST ] (
                    s, Existence.export Post (
                        Responses.Moved.export Post (
                            Responses.Missing.export Post),
                        Preconditions.Common.export Post (
                            Preconditions.Unsafe.export Post (
                                Conflict.export Post (
                                    Operation.export Post POST (
                                        Responses.Created.export Post (
                                            Responses.Other.export Post (
                                                Responses.Common.export Post))))))))

            let export =
                { Metadata =
                    { Name = "http.post"
                      Description = None }
                  Requirements =
                    { Required = set [ "http.core" ]
                      Preconditions = List.empty }
                  Operations =
                    [ Splice (Key [ "http"; "end-decision" ], Right, post) ] }

        (* Put *)

        [<RequireQualifiedAccess>]
        module Put =

            [<Literal>]
            let private Put =
                "put"

            (* Export *)

            let rec private put s =
                Method.export Put [ PUT ] (
                    s, Existence.export Put (
                        Responses.Moved.export Put (
                            continuation),
                        Preconditions.Common.export Put (
                            Preconditions.Unsafe.export Put (
                                Conflict.export Put (
                                    continuation)))))

            and private continuation =
                Operation.export Put PUT (
                    Responses.Created.export Put (
                        Responses.Other.export Put (
                            Responses.Common.export Put)))

            let export =
                { Metadata =
                    { Name = "http.put"
                      Description = None }
                  Requirements =
                    { Required = set [ "http.core" ]
                      Preconditions = List.empty }
                  Operations =
                    [ Splice (Key [ "http"; "end-decision" ], Right, put) ] }

        (* Delete *)

        [<RequireQualifiedAccess>]
        module Delete =

            [<Literal>]
            let private Delete =
                "delete"

            (* Export *)

            let private delete s =
                Method.export Delete [ DELETE ] (
                    s, Existence.export Delete (
                        Responses.Moved.export Delete (
                            Responses.Missing.export Delete),
                        Preconditions.Common.export Delete (
                            Preconditions.Unsafe.export Delete (
                                Operation.export Delete DELETE (
                                    Responses.Common.export Delete)))))

            let export =
                { Metadata =
                    { Name = "http.delete"
                      Description = None }
                  Requirements =
                    { Required = set [ "http.core" ]
                      Preconditions = List.empty }
                  Operations =
                    [ Splice (Key [ "http"; "end-decision" ], Right, delete) ] }

    (* Model *)

    let model =
        Model.create (
            set [
                Core.export
                GetOrHead.export
                Options.export
                Post.export
                Put.export
                Delete.export ])

(* Machine

   Mechanics and implementation of the Hephaestus Machine constructed from the
   Model defined previously. The module wraps the base functions for creating
   Prototypes and Machines from Models present within Hephaestus. *)

[<RequireQualifiedAccess>]
module internal Machine =

    (* Execution *)

    [<RequireQualifiedAccess>]
    module Execution =

        let execute machine =
            Machine.execute machine

    (* Reification *)

    [<RequireQualifiedAccess>]
    module Reification =

        let private prototype, prototypeLog =
            Prototype.createLogged Model.model

        let reify configuration =
            let machine, machineLog = Machine.createLogged prototype configuration

            Execution.execute machine

(* Inference

   Type inference functions for conversion of various forms to a single form,
   for example the conversion of functions and literal values to be
   automatically inferred at compile time to be Literal or Function types of
   Decision.

   This gives a more flexible API where it is used, although at the cost of
   greater documentation/lesser discoverability initially. *)

[<RequireQualifiedAccess>]
module Infer =

    (* Handler *)

    module Handler =

        type Defaults =
            | Defaults

            static member inline Handler (x: Content.Specification -> Freya<Content.Representation>) =
                x

            static member inline Handler (x: Freya<Content.Representation>) =
                fun (_: Content.Specification) -> x

            static member inline Handler (x: Content.Representation) =
                fun (_: Content.Specification) -> Freya.init x

        let inline defaults (a: ^a, _: ^b) =
                ((^a or ^b) : (static member Handler: ^a -> (Content.Specification -> Freya<Content.Representation>)) a)

        let inline infer (x: 'a) =
            defaults (x, Defaults)

    let inline handler v =
        Handler.infer v

    (* Decision (Value<bool>) *)

    module Decision =

        type Defaults =
            | Defaults

            static member Decision (x: Freya<bool>) =
                Dynamic x

            static member Decision (x: bool) =
                Static x

        let inline defaults (a: ^a, _: ^b) =
            ((^a or ^b) : (static member Decision: ^a -> Value<bool>) a)

        let inline infer (x: 'a) =
            defaults (x, Defaults)

    let inline decision v =
        Decision.infer v

    (* Operation (Freya<bool>) *)

    module Operation =

        type Defaults =
            | Defaults

            static member Operation (x: Freya<bool>) =
                x

            static member Operation (x: Freya<unit>) =
                Freya.map (x, fun _ -> true)

        let inline defaults (a: ^a, _: ^b) =
            ((^a or ^b) : (static member Operation: ^a -> Freya<bool>) a)

        let inline infer (x: 'a) =
            defaults (x, Defaults)

    let inline operation v =
        Operation.infer v

    (* Charset list *)

    module Charsets =

        type Defaults =
            | Defaults

            static member Charsets (x: Freya<Charset list>) =
                Dynamic x

            static member Charsets (x: Charset list) =
                Static x

            static member Charsets (x: Charset) =
                Static [ x ]

        let inline defaults (a: ^a, _: ^b) =
            ((^a or ^b) : (static member Charsets: ^a -> Value<Charset list>) a)

        let inline infer (x: 'a) =
            defaults (x, Defaults)

    let inline charsets v =
        Charsets.infer v

    (* ContentCoding list *)

    module ContentCodings =

        type Defaults =
            | Defaults

            static member ContentCodings (x: Freya<ContentCoding list>) =
                Dynamic x

            static member ContentCodings (x: ContentCoding list) =
                Static x

            static member ContentCodings (x: ContentCoding) =
                Static [ x ]

        let inline defaults (a: ^a, _: ^b) =
            ((^a or ^b) : (static member ContentCodings: ^a -> Value<ContentCoding list>) a)

        let inline infer (x: 'a) =
            defaults (x, Defaults)

    let inline contentCodings v =
        ContentCodings.infer v

    (* DateTime *)

    module DateTime =

        type Defaults =
            | Defaults

            static member DateTime (x: Freya<DateTime>) =
                Dynamic x

            static member DateTime (x: DateTime) =
                Static x

        let inline defaults (a: ^a, _: ^b) =
            ((^a or ^b) : (static member DateTime: ^a -> Value<DateTime>) a)

        let inline infer (x: 'a) =
            defaults (x, Defaults)

    let inline dateTime v =
        DateTime.infer v

    (* ETag list *)

    module ETags =

        type Defaults =
            | Defaults

            static member ETags (x: Freya<ETag list>) =
                Dynamic x

            static member ETags (x: ETag list) =
                Static x

            static member ETags (x: ETag) =
                Static [ x ]

        let inline defaults (a: ^a, _: ^b) =
            ((^a or ^b) : (static member ETags: ^a -> Value<ETag list>) a)

        let inline infer (x: 'a) =
            defaults (x, Defaults)

    let inline eTags v =
        ETags.infer v

    (* LanguageTag list *)

    module LanguageTags =

        type Defaults =
            | Defaults

            static member LanguageTags (x: Freya<LanguageTag list>) =
                Dynamic x

            static member LanguageTags (x: LanguageTag list) =
                Static x

            static member LanguageTags (x: LanguageTag) =
                Static [ x ]

        let inline defaults (a: ^a, _: ^b) =
            ((^a or ^b) : (static member LanguageTags: ^a -> Value<LanguageTag list>) a)

        let inline infer (x: 'a) =
            defaults (x, Defaults)

    let inline languageTags v =
        LanguageTags.infer v

    (* MediaType list *)

    module MediaTypes =

        type Defaults =
            | Defaults

            static member MediaTypes (x: Freya<MediaType list>) =
                Dynamic x

            static member MediaTypes (x: MediaType list) =
                Static x

            static member MediaTypes (x: MediaType) =
                Static [ x ]

        let inline defaults (a: ^a, _: ^b) =
            ((^a or ^b) : (static member MediaTypes: ^a -> Value<MediaType list>) a)

        let inline infer (x: 'a) =
            defaults (x, Defaults)

    let inline mediaTypes v =
        MediaTypes.infer v

    (* Method list *)

    module Methods =

        type Defaults =
            | Defaults

            static member Methods (x: Freya<Method list>) =
                Dynamic x

            static member Methods (x: Method list) =
                Static x

            static member Methods (x: Method) =
                Static [ x ]

        let inline defaults (a: ^a, _: ^b) =
            ((^a or ^b) : (static member Methods: ^a -> Value<Method list>) a)

        let inline infer (x: 'a) =
            defaults (x, Defaults)

    let inline methods v =
        Methods.infer v

(* Types

   The base type of an HTTP Machine, representing the user facing type defined
   through the use of the following computation expression.

   The function itself is defined as a single case discriminated union so that
   it can have static members, allowing it to take part in the static inference
   approaches of the basic Freya function, and Pipelines (allowing the pseudo
   typeclass approach which Freya uses in various places for concise APIs). *)

type HttpMachine =
    | HttpMachine of (Configuration -> unit * Configuration)

    (* Common *)

    static member Init _ : HttpMachine =
        HttpMachine (fun c ->
            (), c)

    static member Bind (m: HttpMachine, f: unit -> HttpMachine) : HttpMachine =
        HttpMachine (fun c ->
            let (HttpMachine m) = m
            let (HttpMachine f) = f ()

            (), snd (f (snd (m c))))

    (* Custom *)

    static member Map (m: HttpMachine, f: Configuration -> Configuration) : HttpMachine =
        HttpMachine (fun c ->
            let (HttpMachine m) = m

            (), f (snd (m c)))

    static member inline Set (m: HttpMachine, o, v) =
        HttpMachine (fun c ->
            let (HttpMachine m) = m

            (), Optic.set o (Some v) (snd (m c)))

    (* Typeclasses *)

    static member Freya (HttpMachine machine) : Freya<_> =
            Machine.Reification.reify (snd (machine Configuration.empty))

    static member Pipeline (HttpMachine machine) : Pipeline =
            HttpMachine.Freya (HttpMachine machine)
         *> Pipeline.next

(* Builder

   Computation expression builder for configuring the HTTP Machine, providing a
   simple type-safe syntax and static inference based overloads of single
   functions.
   
   The builder uses the basic configuration builder defined in Freya.Core, which
   only requires the supply of init and bind functions of the appropriate types
   to implement a type suitable for declarative configuration. *)

type HttpMachineBuilder () =

    inherit Configuration.Builder<HttpMachine>
        { Init = HttpMachine.Init
          Bind = HttpMachine.Bind }

(* Syntax

   Custom syntax expressions for the HTTP Machine computation expression
   builder, giving strongly typed syntax for the configuration elements that
   can be set as part of an HTTP Machine. *)

(* Properties

   Configuration for common properties of a the HTTP model which may be used by
   multiple elements/components as part. Multiple implementations may rely on
   the same core declarative property of a resource without needing to be aware
   of the existence of other consumers of that property. *)

(* Request *)

type HttpMachineBuilder with

    [<CustomOperation ("methodsAllowed", MaintainsVariableSpaceUsingBind = true)>]
    member inline __.MethodsAllowed (m, a) =
        HttpMachine.Set (m, Properties.Request.methodsAllowed_, Infer.methods a)

(* Representation *)

type HttpMachineBuilder with

    [<CustomOperation ("charsetsSupported", MaintainsVariableSpaceUsingBind = true)>]
    member inline __.CharsetsSupported (m, a) =
        HttpMachine.Set (m, Properties.Representation.charsetsSupported_, Infer.charsets a)

    [<CustomOperation ("contentCodingsSupported", MaintainsVariableSpaceUsingBind = true)>]
    member inline __.ContentCodingsSupported (m, a) =
        HttpMachine.Set (m, Properties.Representation.contentCodingsSupported_, Infer.contentCodings a)

    [<CustomOperation ("languagesSupported", MaintainsVariableSpaceUsingBind = true)>]
    member inline __.LanguagesSupported (m, a) =
        HttpMachine.Set (m, Properties.Representation.languagesSupported_, Infer.languageTags a)

    [<CustomOperation ("mediaTypesSupported", MaintainsVariableSpaceUsingBind = true)>]
    member inline __.MediaTypesSupported (m, a) =
        HttpMachine.Set (m, Properties.Representation.mediaTypesSupported_, Infer.mediaTypes a)

(* Resource *)

type HttpMachineBuilder with

    [<CustomOperation ("eTags", MaintainsVariableSpaceUsingBind = true)>]
    member inline __.ETags (m, a) =
        HttpMachine.Set (m, Properties.Resource.eTags_, Infer.eTags a)

    [<CustomOperation ("lastModified", MaintainsVariableSpaceUsingBind = true)>]
    member inline __.LastModified (m, a) =
        HttpMachine.Set (m, Properties.Resource.lastModified_, Infer.dateTime a)

(* Elements

   Configuration for discrete elements used to make up specific components used
   within the HTTP model. The elements structure is flattened here to make the
   application of custom syntax more tractable. *)

(* Assertion *)

type HttpMachineBuilder with

    (* Decisions *)

    [<CustomOperation ("serviceAvailable", MaintainsVariableSpaceUsingBind = true)>]
    member inline __.ServiceAvailable (m, decision) =
        HttpMachine.Set (m, Model.Elements.Assertion.Decisions.serviceAvailable_, Infer.decision decision)

    [<CustomOperation ("httpVersionSupported", MaintainsVariableSpaceUsingBind = true)>]
    member inline __.HttpVersionSupported (m, decision) =
        HttpMachine.Set (m, Model.Elements.Assertion.Decisions.httpVersionSupported_, Infer.decision decision)

    (* Terminals *)

    [<CustomOperation ("handleServiceUnavailable", MaintainsVariableSpaceUsingBind = true)>]
    member inline __.HandleServiceUnavailable (m, handler) =
        HttpMachine.Set (m, Model.Elements.Assertion.Terminals.serviceUnavailable_, Infer.handler handler)

    [<CustomOperation ("handleNotImplemented", MaintainsVariableSpaceUsingBind = true)>]
    member inline __.HandleNotImplemented (m, handler) =
        HttpMachine.Set (m, Model.Elements.Assertion.Terminals.notImplemented_, Infer.handler handler)

    [<CustomOperation ("handleNotSupported", MaintainsVariableSpaceUsingBind = true)>]
    member inline __.HandleNotSupported (m, handler) =
        HttpMachine.Set (m, Model.Elements.Assertion.Terminals.httpVersionNotSupported_, Infer.handler handler)

(* Permission *)

type HttpMachineBuilder with

    (* Decisions *)

    [<CustomOperation ("authorized", MaintainsVariableSpaceUsingBind = true)>]
    member inline __.Authorized (m, decision) =
        HttpMachine.Set (m, Model.Elements.Permission.Decisions.authorized_, Infer.decision decision)

    [<CustomOperation ("allowed", MaintainsVariableSpaceUsingBind = true)>]
    member inline __.Allowed (m, decision) =
        HttpMachine.Set (m, Model.Elements.Permission.Decisions.allowed_, Infer.decision decision)

    (* Terminals *)

    [<CustomOperation ("handleUnauthorized", MaintainsVariableSpaceUsingBind = true)>]
    member inline __.HandleUnauthorized (m, handler) =
        HttpMachine.Set (m, Model.Elements.Permission.Terminals.unauthorized_, Infer.handler handler)

    [<CustomOperation ("handleForbidden", MaintainsVariableSpaceUsingBind = true)>]
    member inline __.HandleForbidden (m, handler) =
        HttpMachine.Set (m, Model.Elements.Permission.Terminals.forbidden_, Infer.handler handler)

(* Validation *)

type HttpMachineBuilder with

    (* Decisions *)

    [<CustomOperation ("badRequest", MaintainsVariableSpaceUsingBind = true)>]
    member inline __.BadRequest (m, decision) =
        HttpMachine.Set (m, Model.Elements.Validation.Decisions.badRequest_, Infer.decision decision)

    [<CustomOperation ("uriTooLong", MaintainsVariableSpaceUsingBind = true)>]
    member inline __.UriTooLong (m, decision) =
        HttpMachine.Set (m, Model.Elements.Validation.Decisions.uriTooLong_, Infer.decision decision)

    (* Terminals *)

    [<CustomOperation ("handleBadRequest", MaintainsVariableSpaceUsingBind = true)>]
    member inline __.HandleBadRequest (m, handler) =
        HttpMachine.Set (m, Model.Elements.Validation.Terminals.badRequest_, Infer.handler handler)

    [<CustomOperation ("handleExpectationFailed", MaintainsVariableSpaceUsingBind = true)>]
    member inline __.HandleExpectationFailed (m, handler) =
        HttpMachine.Set (m, Model.Elements.Validation.Terminals.expectationFailed_, Infer.handler handler)

    [<CustomOperation ("handleMethodNotAllowed", MaintainsVariableSpaceUsingBind = true)>]
    member inline __.HandleMethodNotAllowed (m, handler) =
        HttpMachine.Set (m, Model.Elements.Validation.Terminals.methodNotAllowed_, Infer.handler handler)

    [<CustomOperation ("handleUriTooLong", MaintainsVariableSpaceUsingBind = true)>]
    member inline __.HandleUriTooLong (m, handler) =
        HttpMachine.Set (m, Model.Elements.Validation.Terminals.uriTooLong_, Infer.handler handler)

(* Negotiation *)

type HttpMachineBuilder with

    (* Terminals *)

    [<CustomOperation ("handleNotAcceptable", MaintainsVariableSpaceUsingBind = true)>]
    member inline __.HandleNotAcceptable (m, handler) =
        HttpMachine.Set (m, Model.Elements.Negotiation.Terminals.notAcceptable_, Infer.handler handler)

(* Existence *)

type HttpMachineBuilder with

    (* Decisions *)

    [<CustomOperation ("exists", MaintainsVariableSpaceUsingBind = true)>]
    member inline __.Exists (m, decision) =
        HttpMachine.Set (m, Model.Elements.Existence.Decisions.exists_, Infer.decision decision)

(* Preconditions *)

type HttpMachineBuilder with

    (* Terminals *)

    [<CustomOperation ("handlePreconditionFailed", MaintainsVariableSpaceUsingBind = true)>]
    member inline __.HandlePreconditionFailed (m, handler) =
        HttpMachine.Set (m, Model.Elements.Preconditions.Shared.Terminals.preconditionFailed_, Infer.handler handler)

(* Conflict *)

type HttpMachineBuilder with

    (* Decisions *)

    [<CustomOperation ("conflict", MaintainsVariableSpaceUsingBind = true)>]
    member inline __.Conflict (m, decision) =
        HttpMachine.Set (m, Model.Elements.Conflict.Decisions.conflict_, Infer.decision decision)

    (* Terminals *)

    [<CustomOperation ("handleConflict", MaintainsVariableSpaceUsingBind = true)>]
    member inline __.HandleConflict (m, handler) =
        HttpMachine.Set (m, Model.Elements.Conflict.Terminals.conflict_, Infer.handler handler)

(* Operation *)

type HttpMachineBuilder with

    (* Decisions *)

    [<CustomOperation ("completed", MaintainsVariableSpaceUsingBind = true)>]
    member inline __.Completed (m, decision) =
        HttpMachine.Set (m, Model.Elements.Operation.Decisions.completed_, Infer.decision decision)

    (* Operations *)

    [<CustomOperation ("doDelete", MaintainsVariableSpaceUsingBind = true)>]
    member inline __.DoDelete (m, a) =
        HttpMachine.Set (m, (Model.Elements.Operation.Decisions.operationMethod_ DELETE), Infer.operation a)

    [<CustomOperation ("doPost", MaintainsVariableSpaceUsingBind = true)>]
    member inline __.DoPost (m, a) =
        HttpMachine.Set (m, (Model.Elements.Operation.Decisions.operationMethod_ POST), Infer.operation a)

    [<CustomOperation ("doPut", MaintainsVariableSpaceUsingBind = true)>]
    member inline __.DoPut (m, a) =
        HttpMachine.Set (m, (Model.Elements.Operation.Decisions.operationMethod_ PUT), Infer.operation a)

    (* Terminals *)

    [<CustomOperation ("handleAccepted", MaintainsVariableSpaceUsingBind = true)>]
    member inline __.HandleAccepted (m, handler) =
        HttpMachine.Set (m, Model.Elements.Operation.Terminals.accepted_, Infer.handler handler)

    [<CustomOperation ("handleInternalServerError", MaintainsVariableSpaceUsingBind = true)>]
    member inline __.HandleInternalServerError (m, handler) =
        HttpMachine.Set (m, Model.Elements.Operation.Terminals.internalServerError_, Infer.handler handler)

(* Responses.Common *)

type HttpMachineBuilder with

    (* Terminals *)

    [<CustomOperation ("handleOk", MaintainsVariableSpaceUsingBind = true)>]
    member inline __.HandleOk (m, handler) =
        HttpMachine.Set (m, Model.Elements.Responses.Common.Terminals.ok_, Infer.handler handler)

(* Responses.Created *)

type HttpMachineBuilder with

    (* Decisions *)

    [<CustomOperation ("created", MaintainsVariableSpaceUsingBind = true)>]
    member inline __.Created (m, decision) =
        HttpMachine.Set (m, Model.Elements.Responses.Created.Decisions.created_, Infer.decision decision)

    (* Terminals *)

    [<CustomOperation ("handleCreated", MaintainsVariableSpaceUsingBind = true)>]
    member inline __.HandleCreated (m, handler) =
        HttpMachine.Set (m, Model.Elements.Responses.Created.Terminals.created_, Infer.handler handler)

(* Responses.Missing *)

type HttpMachineBuilder with

    (* Terminals *)

    [<CustomOperation ("handleNotFound", MaintainsVariableSpaceUsingBind = true)>]
    member inline __.HandleNotFound (m, handler) =
        HttpMachine.Set (m, Model.Elements.Responses.Missing.Terminals.notFound_, Infer.handler handler)

(* Responses.Moved *)

type HttpMachineBuilder with

    (* Decisions *)

    [<CustomOperation ("gone", MaintainsVariableSpaceUsingBind = true)>]
    member inline __.Gone (m, decision) =
        HttpMachine.Set (m, Model.Elements.Responses.Moved.Decisions.gone_, Infer.decision decision)

    [<CustomOperation ("movedPermanently", MaintainsVariableSpaceUsingBind = true)>]
    member inline __.MovedPermanently (m, decision) =
        HttpMachine.Set (m, Model.Elements.Responses.Moved.Decisions.movedPermanently_, Infer.decision decision)

    [<CustomOperation ("movedTemporarily", MaintainsVariableSpaceUsingBind = true)>]
    member inline __.MovedTemporarily (m, decision) =
        HttpMachine.Set (m, Model.Elements.Responses.Moved.Decisions.movedTemporarily_, Infer.decision decision)

    (* Terminals *)

    [<CustomOperation ("handleGone", MaintainsVariableSpaceUsingBind = true)>]
    member inline __.HandleGone (m, handler) =
        HttpMachine.Set (m, Model.Elements.Responses.Moved.Terminals.gone_, Infer.handler handler)

    [<CustomOperation ("handleMovedPermanently", MaintainsVariableSpaceUsingBind = true)>]
    member inline __.HandleMovedPermanently (m, handler) =
        HttpMachine.Set (m, Model.Elements.Responses.Moved.Terminals.movedPermanently_, Infer.handler handler)

    [<CustomOperation ("handleTemporaryRedirect", MaintainsVariableSpaceUsingBind = true)>]
    member inline __.HandleTemporaryRedirect (m, handler) =
        HttpMachine.Set (m, Model.Elements.Responses.Moved.Terminals.temporaryRedirect_, Infer.handler handler)

(* Responses.Options *)

type HttpMachineBuilder with

    (* Terminals *)

    [<CustomOperation ("handleOptions", MaintainsVariableSpaceUsingBind = true)>]
    member inline __.HandleOptions (m, handler) =
        HttpMachine.Set (m, Model.Elements.Responses.Options.Terminals.options_, Infer.handler handler)

(* Responses.Other *)

type HttpMachineBuilder with

    (* Decisions *)

    [<CustomOperation ("found", MaintainsVariableSpaceUsingBind = true)>]
    member inline __.Found (m, decision) =
        HttpMachine.Set (m, Model.Elements.Responses.Other.Decisions.found_, Infer.decision decision)

    [<CustomOperation ("multipleChoices", MaintainsVariableSpaceUsingBind = true)>]
    member inline __.MultipleChoices (m, decision) =
        HttpMachine.Set (m, Model.Elements.Responses.Other.Decisions.multipleChoices_, Infer.decision decision)

    [<CustomOperation ("seeOther", MaintainsVariableSpaceUsingBind = true)>]
    member inline __.SeeOther (m, decision) =
        HttpMachine.Set (m, Model.Elements.Responses.Other.Decisions.seeOther_, Infer.decision decision)

    (* Terminals *)

    [<CustomOperation ("handleFound", MaintainsVariableSpaceUsingBind = true)>]
    member inline __.HandleFound (m, handler) =
        HttpMachine.Set (m, Model.Elements.Responses.Other.Terminals.found_, Infer.handler handler)

    [<CustomOperation ("handleMultipleChoices", MaintainsVariableSpaceUsingBind = true)>]
    member inline __.HandleMultipleChoices (m, handler) =
        HttpMachine.Set (m, Model.Elements.Responses.Other.Terminals.multipleChoices_, Infer.handler handler)

    [<CustomOperation ("handleSeeOther", MaintainsVariableSpaceUsingBind = true)>]
    member inline __.HandleSeeOther (m, handler) =
        HttpMachine.Set (m, Model.Elements.Responses.Other.Terminals.seeOther_, Infer.handler handler)

(* Expressions

   Computation expressions, instances of the HTTP Machine computation
   expression builder. The fully named instance, freyaHttpMachine is aliased to
   freyaMachine to provide the possibility of more concise code when only one
   kind of machine is in scope.

   This naming also matches the original single form approach to machines, and
   provides backwards compatibility. *)

let freyaHttpMachine =
    HttpMachineBuilder ()

let freyaMachine =
    freyaHttpMachine