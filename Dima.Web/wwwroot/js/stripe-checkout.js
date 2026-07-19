window.checkout = url => {
    if (!url) {
        throw new Error("URL de checkout do Stripe não informada.");
    }

    window.location.href = url;
};