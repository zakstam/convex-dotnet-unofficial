// Query to get the current authenticated user's information.

import { query } from "../_generated/server";
import { getAuthUser } from "../auth";

export default query({
  args: {},
  handler: async (ctx) => {
    const user = await getAuthUser(ctx);

    if (!user) {
      return null;
    }

    return {
      id: user._id,
      email: user.email,
      name: user.name,
      username: user.name || user.email?.split("@")[0] || "Anonymous",
      image: user.image,
      emailVerified: user.emailVerified,
    };
  },
});
