(function () {

    // enable jquery validate to check for multip csv emails
    $.validator.addMethod("multiemail", function (value, element) {

        var valid, emails, i, limit;

        if (this.optional(element)) {
            return true;
        }

        emails = value.split(',');
        valid = true;

        for (i = 0, limit = emails.length; i < limit; ++i) {
            value = emails[i];
            valid = valid && $.validator.methods.email.call(this, value, element);
        }

        return valid;
    }, "Enter only valid email addresses separated by commas; no spaces and no trailing commas please");

} ());