﻿namespace Freya.Machines.Http.Cors

open Freya.Machines

#nowarn "46"

(* Preflight *)

[<AutoOpen>]
module Extension =

    (* Extension *)

    let cors =
        set [ Cors.component ]