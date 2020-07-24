function configure_kerberos_server_in_krb5_file() {
    if [ "$#" -ne 2 ]; then
        echo -e "\e[1;31mconfigure_kerberos_server_in_krb5_file not used correctly! Provide two parameters (public url of kerberos server and kerberos realm) \e[0m"
    else
        echo "[realms]
        "$1" = {
        admin_server="$2"
        kdc="$2"
        }" >>"$CONF_FILES"/krb5.conf
        awk -v kerberos_realm=${1} '/default_realm/{c++;if(c==1){sub("default_realm.*","default_realm="kerberos_realm);c=0}}1' "$CONF_FILES"/krb5.conf >/tmp/tmpfile && mv /tmp/tmpfile /etc/krb5.conf
    fi
}