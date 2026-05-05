namespace FolioIQ

open WebSharper
open WebSharper.Web

module Auth =

    let hashPassword (password: string) =
        BCrypt.Net.BCrypt.HashPassword(password)

    let verifyPassword (password: string) (hash: string) =
        BCrypt.Net.BCrypt.Verify(password, hash)

    let loginUser (userId: int) =
        async {
            let ctx = WebSharper.Web.Remoting.GetContext()
            return! ctx.UserSession.LoginUser(string userId)
        }

    let logoutUser () =
        async {
            let ctx = WebSharper.Web.Remoting.GetContext()
            return! ctx.UserSession.Logout()
        }

    let getLoggedInUserId () =
        async {
            let ctx = WebSharper.Web.Remoting.GetContext()
            let! user = ctx.UserSession.GetLoggedInUser()
            return user |> Option.map int
        }

    let requireAuth () =
        async {
            let! userIdOpt = getLoggedInUserId()
            match userIdOpt with
            | Some userId -> return userId
            | None -> return failwith "Unauthorized"
        }

// working on fix double dispose on httpclient in stock api...