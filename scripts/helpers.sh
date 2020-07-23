function set_principal_in_jaas_file() {
    if [ "$#" -ne 2 ]; then
        echo -e "\e[1;31mset_principal_in_jaas_file not used correctly! Provide two parameters! First is jaas file path. Second is the new principal value\e[0m"
    else
        # Escaping argument 2 for special characters
        sed -i -r -E "/principal=/ s/=.*/=\"$(echo $2 | sed -e 's#\([]*^+.$[/]\)#\\\1#g')\";/" $1
    fi
}

function configure_kerberos_server_in_krb5_file() {
    if [ "$#" -ne 2 ]; then
        echo -e "\e[1;31mconfigure_kerberos_server_in_krb5_file not used correctly! Provide two parameters (public url of kerberos server and kerberos realm) \e[0m"
    else
        echo "[realms]
        "$1" = {
        admin_server="$2"
        kdc="$2"
        }" >>"$KAFKA_HOME"/config/krb5.conf
        awk -v kerberos_realm=${1} '/default_realm/{c++;if(c==1){sub("default_realm.*","default_realm="kerberos_realm);c=0}}1' "$KAFKA_HOME"/config/krb5.conf >/tmp/tmpfile && mv /tmp/tmpfile "$KAFKA_HOME"/config/krb5.conf
    fi
}
