window.checkout = async (stripePublicKey, sessionId) => {
    console.log("Stripe public key:", stripePublicKey);
    console.log("Session ID:", sessionId);

    const stripe = Stripe(stripePublicKey);

    const result = await stripe.redirectToCheckout({
        sessionId: sessionId
    });

    if (result.error) {
        console.error("Stripe checkout error:", result.error.message);
        throw new Error(result.error.message);
    }
}