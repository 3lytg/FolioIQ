namespace FolioIQ

open WebSharper

module Server =

    [<Rpc>]
    let DoSomething input =
        let R (s: string) = System.String(Array.rev(s.ToCharArray()))
        async {
            return R input
        }

// working on add user registration and password hashing...