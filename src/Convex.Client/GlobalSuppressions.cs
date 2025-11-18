// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.

using System.Diagnostics.CodeAnalysis;

// CA1716: Resume is an intentional API design choice despite being a keyword in some languages
[assembly: SuppressMessage("Naming", "CA1716:Identifiers should not match keywords",
    Justification = "Resume is the semantically correct name for resuming a paginated subscription",
    Scope = "member", Target = "~M:Convex.Client.Pagination.ILivePaginatedSubscription`1.Resume")]
